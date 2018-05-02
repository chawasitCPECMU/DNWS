using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using OpenCL.Net.Extensions;
using OpenCL.Net;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Net;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace DNWS
{
  class GPUPlugin : IPluginWithParameters
  {
    private Context _context;
    private Device _device;
    private Dictionary<string, string> _parameters;
    private bool _isInit = false;

    public GPUPlugin()
    {
    }

    private void init()
    {
      ErrorCode error;

      // Get platform info
      Platform[] platforms = Cl.GetPlatformIDs(out error);
      List<Device> devicesList = new List<Device>();

      LogError (error, "Cl.GetPlaformIDs");

      DeviceType deviceType = DeviceType.Default;
      switch(_parameters["DeviceType"]) {
        case "Gpu":
          deviceType = DeviceType.Gpu;
          break;
        case "Cpu":
          deviceType = DeviceType.Cpu;
          break;
        case "All":
          deviceType = DeviceType.All;
          break;
        case "Accelerator":
          deviceType = DeviceType.Accelerator;
          break;
      }

      // Get available devices
      foreach (Platform platform in platforms) {
        string platformName = Cl.GetPlatformInfo(platform, PlatformInfo.Name, out error).ToString();
        Console.WriteLine("Platform: " + platformName);
        LogError (error, "Cl.GetPlatformInfo");
        foreach (Device device in Cl.GetDeviceIDs(platform, deviceType, out error)) {
          LogError(error, "Cl.GetDeviceIDs");
          Console.WriteLine("Device:" + device.ToString() );
          devicesList.Add(device);
        }
      }

      if(devicesList.Count <= 0) {
        Console.WriteLine("No devices found.");
        return;
      }

      _device = devicesList[0];
      if(Cl.GetDeviceInfo(_device, DeviceInfo.ImageSupport, out error).CastTo<OpenCL.Net.Bool>() == OpenCL.Net.Bool.False)
      {
        Console.WriteLine("No image support.");
        return;
      }
      _context = Cl.CreateContext(null, 1, new[] {_device}, ContextNotify, IntPtr.Zero, out error);
      LogError(error, "Cl.CreateContext");


    }

    private void ContextNotify(string errInfo, byte[] data, IntPtr cb, IntPtr userData) {
        Console.WriteLine("OpenCL Notification: " + errInfo);
    }

    public void PreProcessing(HTTPRequest request)
    {
      throw new NotImplementedException();
    }

    private void LogError(ErrorCode err, string name)
    {
      if (err != ErrorCode.Success) {
          Console.WriteLine("ERROR: " + name + " (" + err.ToString() + ")");
      }
    }

    private StringBuilder GenUploadForm()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("<form method=\"post\">");
        sb.Append("Image URL:");
        sb.Append("<input type=\"text\" name=\"imageUploadUrl\" id=\"imageUploadUrl\" />");
        sb.Append("<input type=\"submit\" value=\"Retrieve Image\" name=\"submit\" />");
        sb.Append("</form>");
        return sb;
    }

    private byte[] DownloadImageFromUrl(string url)
    {
      byte[] data = null;
      try {
        WebRequest req = WebRequest.Create(url);
        WebResponse response = req.GetResponse();
        Stream stream = response.GetResponseStream();
        MemoryStream memStream = new MemoryStream();
        int total = 0;
        byte[] buffer = new byte[1024];
        while(true) {
          int bytesRead = stream.Read(buffer, 0, buffer.Length);
          total += bytesRead;
          memStream.Write(buffer, 0, bytesRead);
          if(bytesRead == 0) break;
        }
        data = memStream.ToArray();
      } catch (Exception ex) {
        throw ex;
      }
      return data;
    }
    public HTTPResponse GetResponse(HTTPRequest request)
    {
      HTTPResponse response = new HTTPResponse(200);
      StringBuilder sb = new StringBuilder();
      ErrorCode error;

      if(!_isInit) {
        init();
        _isInit = true;
      }

      if (request.Method == HTTPRequest.METHOD_GET) {
        // Input form, this can be place by any HTML page
        sb.Append("<html><body>");
        sb.Append(GenUploadForm());
        sb.Append("</body></html>");
        response.Body = Encoding.UTF8.GetBytes(sb.ToString());
        return response;
      } else if (request.Method == HTTPRequest.METHOD_POST) {
        // Get remote image from URL
        string url = Uri.UnescapeDataString(request.GetRequestByKey("imageUploadUrl"));
        byte[] data;
        try {
          data = DownloadImageFromUrl(url);
        } catch (Exception) {
          return new HTTPResponse(400);
        }
        // https://www.codeproject.com/Articles/502829/GPGPU-image-processing-basics-using-OpenCL-NET
        // Convert image to bitmap binary
        Image inputImage = Image.FromStream(new MemoryStream(data));
        if (inputImage == null) {
          return new HTTPResponse(500);
        }
        int imagewidth = inputImage.Width;
        int imageHeight = inputImage.Height;

        Bitmap bmpImage = new Bitmap(inputImage);
        BitmapData bitmapData = bmpImage.LockBits(new Rectangle(0, 0, bmpImage.Width, bmpImage.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        int inputImageByteSize = bitmapData.Stride * bitmapData.Height;
        byte[] inputByteArray = new byte[inputImageByteSize];
        Marshal.Copy(bitmapData.Scan0, inputByteArray, 0, inputImageByteSize);

        // Load kernel source code
        string programPath = System.Environment.CurrentDirectory +  "/Kernel.cl";
        if(!System.IO.File.Exists(programPath)) {
          return new HTTPResponse(404);
        }

        string programSource = System.IO.File.ReadAllText(programPath);
        using (OpenCL.Net.Program program = Cl.CreateProgramWithSource(_context, 1, new[] {programSource}, null, out error)) {
          // Create kernel
          LogError(error, "Cl.CreateProgramWithSource");
          error = Cl.BuildProgram(program, 1, new[] {_device}, string.Empty, null, IntPtr.Zero);
          LogError(error, "Cl.BuildProgram");
          if (Cl.GetProgramBuildInfo (program, _device, ProgramBuildInfo.Status, out error).CastTo<OpenCL.Net.BuildStatus>()
              != BuildStatus.Success) {
            LogError(error, "Cl.GetProgramBuildInfo");
            return new HTTPResponse(404);
          }
          Kernel kernel = Cl.CreateKernel(program, "imagingTest", out error);
          LogError(error, "Cl.CreateKernel");

          // Create image memory objects
          OpenCL.Net.ImageFormat clImageFormat = new OpenCL.Net.ImageFormat(ChannelOrder.RGBA, ChannelType.Unsigned_Int8);
          IMem inputImage2DBuffer = Cl.CreateImage2D(_context, MemFlags.CopyHostPtr | MemFlags.ReadOnly,
                                    clImageFormat, (IntPtr) bitmapData.Width, (IntPtr) bitmapData.Height,
                                    (IntPtr)0, inputByteArray, out error);
          LogError(error, "CreateImage2D input");
          byte[] outputByteArray = new byte[inputImageByteSize];
          IMem outputImage2DBuffer = Cl.CreateImage2D(_context, MemFlags.CopyHostPtr | MemFlags.WriteOnly,
                                    clImageFormat, (IntPtr) bitmapData.Width, (IntPtr) bitmapData.Height,
                                    (IntPtr) 0, outputByteArray, out error);
          LogError(error, "CreateImage2D output");

          // Set arguments
          int IntPtrSize = Marshal.SizeOf(typeof(IntPtr)); 
          error = Cl.SetKernelArg(kernel, 0, (IntPtr)IntPtrSize, inputImage2DBuffer);
          error |= Cl.SetKernelArg(kernel, 1, (IntPtr)IntPtrSize, outputImage2DBuffer);
          LogError(error, "Cl.SetKernelArg");

          // Create command queue
          CommandQueue cmdQueue = Cl.CreateCommandQueue(_context, _device, (CommandQueueProperties)0, out error);
          LogError(error, "Cl.CreateCommandQueue");
          Event clevent;

          // Copy input image from the host to the GPU
          IntPtr[] originPtr = new IntPtr[] { (IntPtr) 0, (IntPtr) 0, (IntPtr) 0};
          IntPtr[] regionPtr = new IntPtr[] { (IntPtr) imagewidth, (IntPtr) imageHeight, (IntPtr) 1};
          IntPtr[] workGroupSizePtr = new IntPtr[] { (IntPtr) imagewidth, (IntPtr) imageHeight, (IntPtr) 1};
          error = Cl.EnqueueWriteImage(cmdQueue, inputImage2DBuffer, Bool.True, originPtr, regionPtr, (IntPtr) 0,
                  (IntPtr) 0, inputByteArray, 0, null, out clevent);
          LogError(error, "Cl.EnqueueWriteImage");

          // Run the kernel
          error = Cl.EnqueueNDRangeKernel(cmdQueue, kernel, 2, null, workGroupSizePtr, null, 0, null, out clevent);
          LogError(error, "Cl.EnqueueNDRangeKernel");

          // Wait for finish event
          error = Cl.Finish(cmdQueue);
          LogError(error, "Cl.Finish");

          // Read the output image back from GPU
          error = Cl.EnqueueReadImage(cmdQueue, outputImage2DBuffer, Bool.True, originPtr, regionPtr, (IntPtr) 0,
                  (IntPtr)0, outputByteArray, 0, null, out clevent);
          LogError(error, "Cl.EnqueueReadImage");
          error = Cl.Finish(cmdQueue);
          LogError(error, "Cl.Finih");

          // Release memory
          Cl.ReleaseKernel(kernel);
          Cl.ReleaseCommandQueue(cmdQueue);
          Cl.ReleaseMemObject(inputImage2DBuffer);
          Cl.ReleaseMemObject(outputImage2DBuffer);

          // Convert binary bitmap to JPEG image and return as response
          GCHandle pinnedOutputArray = GCHandle.Alloc(outputByteArray, GCHandleType.Pinned);
          IntPtr outputBmpPointer = pinnedOutputArray.AddrOfPinnedObject();
          Bitmap outputBitmap = new Bitmap(imagewidth, imageHeight, bitmapData.Stride, PixelFormat.Format32bppArgb, outputBmpPointer);
          MemoryStream msOutput = new MemoryStream();
          outputBitmap.Save(msOutput, System.Drawing.Imaging.ImageFormat.Jpeg);
          response.Body = msOutput.ToArray();
          response.Type = "image/jpeg";
          return response;
        }  
      }
      return new HTTPResponse(501);
    }

    public HTTPResponse PostProcessing(HTTPResponse response)
    {
      throw new NotImplementedException();
    }

    public void SetParameters(Dictionary<string, string> parameters)
    {
      _parameters = parameters;
    }
  }
}