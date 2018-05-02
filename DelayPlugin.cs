using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace DNWS
{
  class DelayPlugin : IPlugin
  {
    public DelayPlugin()
    {
    }

    public void PreProcessing(HTTPRequest request)
    {
      throw new NotImplementedException();
    }
    public HTTPResponse GetResponse(HTTPRequest request)
    {
      HTTPResponse response = null;
      String[] parts = Regex.Split(request.Filename, "[?]");
      Int32 delay = 0;
      if (parts.Length > 1) {
        try {
          delay = Convert.ToInt32(parts[1]);
        } catch (Exception ex) {
          response = new HTTPResponse(400);
          response.Body = Encoding.UTF8.GetBytes(ex.ToString());
          return response;
        }

      } 
      StringBuilder sb = new StringBuilder();
      response = new HTTPResponse(200);
      Thread.Sleep(delay);

      sb.Append("<html><body>");
      sb.Append("<h1>Sleep for " + delay + " millisecond. </h1>");
      sb.Append("</body></html>");
      response.Body = Encoding.UTF8.GetBytes(sb.ToString());
      return response;
    }

    public HTTPResponse PostProcessing(HTTPResponse response)
    {
      throw new NotImplementedException();
    }
  }
}