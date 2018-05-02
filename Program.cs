using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using Microsoft.Extensions.Configuration;
using System.Threading;

namespace DNWS
{
    // Main class
    public class Program
    {
        static public IConfigurationRoot Configuration { get; set; }

        // Log to console
        public void Log(String msg)
        {
            Console.WriteLine(msg);
        }

        // Start the server, Singleton here
        public void Start()
        {
            // Start server
            var builder = new ConfigurationBuilder();
            builder.SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("config.json");
            Configuration = builder.Build();
            DotNetWebServer ws = DotNetWebServer.GetInstance(this);
            ws.Start();
        }

        static void Main(string[] args)
        {
            Program p = new Program();
            p.Start();
        }
    }

    public class PluginInfo
    {
        protected string _path;
        protected string _type;
        protected bool _preprocessing;
        protected bool _postprocessing;
        protected IPlugin _reference;
        protected Dictionary<string, string> _parameters;

        public string path
        {
            get { return _path;}
            set {_path = value;}
        }
        public string type
        {
            get { return _type;}
            set {_type = value;}
        }
        public bool preprocessing
        {
            get { return _preprocessing;}
            set {_preprocessing = value;}
        }
        public bool postprocessing
        {
            get { return _postprocessing;}
            set {_postprocessing = value;}
        }
        public IPlugin reference
        {
            get { return _reference;}
            set {_reference = value;}
        }

        public Dictionary<string,string> parameters
        {
            get {return _parameters;}
            set {_parameters = value;}
        }

    }

    public class PluginManager
    {
        private static PluginManager _instance = null;
        private Dictionary<string, PluginInfo> plugins = null;
        private Program _parent;

        private PluginManager()
        {

        }

        private void SetParent(Program parent)
        {
            _parent = parent;
        }

        /* Singletron
         */
        public static PluginManager GetInstance(Program parent)
        {
            if (_instance == null) {
                _instance = new PluginManager();
            }
            _instance.SetParent(parent);
            return _instance;
        }

        public Dictionary<string, PluginInfo> Plugins
        {
            get
            {
                return plugins;
            }
        }

        public void LoadConfiguration(IEnumerable<IConfigurationSection> sections)
        {
            if (plugins == null)
            {
                plugins = new Dictionary<string, PluginInfo>();
                foreach (ConfigurationSection section in sections)
                {
                    PluginInfo pi = new PluginInfo();
                    Dictionary<string, string> parameters = null;
                    pi.path = section["Path"];
                    pi.type = section["Class"];
                    pi.preprocessing = section["Preprocessing"].ToLower().Equals("true");
                    pi.postprocessing = section["Postprocessing"].ToLower().Equals("true");
                    foreach(ConfigurationSection parameter in section.GetSection("Parameters").GetChildren()) {
                        if (parameters == null) parameters = new Dictionary<string,string>();
                        parameters[parameter.Key] = parameter.Value;
                    }
                    try {
                        if(parameters != null) {
                            IPluginWithParameters ip = (IPluginWithParameters)Activator.CreateInstance(Type.GetType(pi.type));
                            ip.SetParameters(parameters);
                            pi.reference = (IPlugin) ip;
                        } else {
                            pi.reference = (IPlugin)Activator.CreateInstance(Type.GetType(pi.type));
                        }
                    } catch (Exception ex) {
                        _parent.Log("Error loading plugin " + pi.path + " with error " + ex);
                        continue;
                    }
                    plugins[section["Path"]] = pi;
                    _parent.Log("Plugin " + pi.path + " loaded.");
                }
            }
        }
    }
    /// <summary>
    /// HTTP processor will process each http request
    /// </summary>

    public class HTTPProcessor
    {
        // Get config from config manager, e.g., document root and port
        protected string ROOT = Program.Configuration["DocumentRoot"];
        protected Socket _client;
        protected Program _parent;
        protected PluginManager PM;

        /// <summary>
        /// Constructor, set the client socket and parent ref, also init stat hash
        /// </summary>
        /// <param name="client">Client socket</param>
        /// <param name="parent">Parent ref</param>
        public HTTPProcessor(Socket client, Program parent)
        {
            _client = client;
            _parent = parent;
            // load plugins
            PM = PluginManager.GetInstance(_parent);
            PM.LoadConfiguration(Program.Configuration.GetSection("Plugins").GetChildren());
        }

        /// <summary>
        /// Get a file from local harddisk based on path
        /// </summary>
        /// <param name="path">Absolute path to the file</param>
        /// <returns></returns>
        protected HTTPResponse getFile(String path)
        {
            HTTPResponse response = null;

            // Guess the content type from file extension
            string fileType = "text/html";
            if (path.ToLower().EndsWith(".jpg") || path.ToLower().EndsWith(".jpeg"))
            {
                fileType = "image/jpeg";
            }
            else if (path.ToLower().EndsWith(".png"))
            {
                fileType = "image/png";
            }
            else if (path.ToLower().EndsWith(".js"))
            {
                fileType = "application/javascript";
            }
            else if (path.ToLower().EndsWith(".css"))
            {
                fileType = "text/css";
            }

            // Try to read the file, if not found then 404, otherwise, 500.
            try
            {
                response = new HTTPResponse(200);
                response.Type = fileType;
                response.Body = System.IO.File.ReadAllBytes(path);
            }
            catch (FileNotFoundException ex)
            {
                response = new HTTPResponse(404);
                response.Body = Encoding.UTF8.GetBytes("<h1>404 Not found</h1>" + ex.Message);
            }
            catch (Exception ex)
            {
                response = new HTTPResponse(500);
                response.Body = Encoding.UTF8.GetBytes("<h1>500 Internal Server Error</h1>" + ex.Message);
            }
            return response;

        }

        /// <summary>
        /// Get a request from client, process it, then return response to client
        /// </summary>
        public void Process()
        {
            NetworkStream ns = new NetworkStream(_client);
            StringBuilder requestStr = new StringBuilder();
            HTTPRequest request = null;
            HTTPResponse response = null;
            byte[] bytes = new byte[1024];
            int bytesRead;
            

            // Read all request
            do
            {
                bytesRead = ns.Read(bytes, 0, bytes.Length);
                requestStr.Append(Encoding.UTF8.GetString(bytes, 0, bytesRead));
            } while (ns.DataAvailable);

            request = new HTTPRequest(requestStr.ToString());
            request.AddProperty("RemoteEndPoint", _client.RemoteEndPoint.ToString());

            // We can handle only GET now
            if(request.Status != 200) {
                response = new HTTPResponse(request.Status);
            }
            else
            {
                bool processed = false;
                //FIXME, this seem duplicate with HTTPRequest
                string[] requestUrls = request.Url.Split("/");
                string[] paths = requestUrls[1].Split("?");
                // pre processing
                foreach(KeyValuePair<string, PluginInfo> plugininfo in PM.Plugins) {
                    if(plugininfo.Value.preprocessing) {
                        plugininfo.Value.reference.PreProcessing(request);
                    }
                }
                // plugins
                foreach(KeyValuePair<string, PluginInfo> plugininfo in PM.Plugins) {
                    if(paths[0].Equals(plugininfo.Key, StringComparison.InvariantCultureIgnoreCase)) {
                    //if(request.Url.StartsWith("/" + plugininfo.Key)) {
                        response = plugininfo.Value.reference.GetResponse(request);
                        processed = true;
                    }
                }
                // local file
                if(!processed) {
                    if (request.Filename.Equals(""))
                    {
                        response = getFile(ROOT + "/" + request.Url + "/index.html");
                    }
                    else
                    {
                        response = getFile(ROOT + "/" + request.Url);
                    }
                }
                // post processing pipe
                foreach(KeyValuePair<string, PluginInfo> plugininfo in PM.Plugins) {
                    if(plugininfo.Value.postprocessing) {
                        response = plugininfo.Value.reference.PostProcessing(response);
                    }
                }
            }
            // Generate response
            ns.Write(Encoding.UTF8.GetBytes(response.Header), 0, response.Header.Length);
            if(response.Body != null) {
              ns.Write(response.Body, 0, response.Body.Length);
            }

            // Shuting down
            //ns.Close();
            _client.Shutdown(SocketShutdown.Both);
            //_client.Close();

        }
    }

    public class TaskInfo
    {
        private HTTPProcessor _hp;
        public HTTPProcessor hp 
        { 
            get {return _hp;}
            set {_hp = value;}
        }
        public TaskInfo(HTTPProcessor hp)
        {
            this.hp = hp;
        }
    }

    /// <summary>
    /// Main server class, open the socket and wait for client
    /// </summary>
    public class DotNetWebServer
    {
        protected int _port;
        protected int _maxThread;
        protected String _threadModel;
        protected Program _parent;
        protected Socket serverSocket;
        protected Socket clientSocket;
        private static DotNetWebServer _instance = null;
        protected int id;

        private DotNetWebServer(Program parent)
        {
            _parent = parent;
            id = 0;
        }

        /// <summary>
        /// Singleton here
        /// </summary>
        /// <param name="parent">parent ref</param>
        /// <returns></returns>
        public static DotNetWebServer GetInstance(Program parent)
        {
            if (_instance == null)
            {
                _instance = new DotNetWebServer(parent);
            }
            return _instance;
        }

        public void ThreadProc(Object stateinfo)
        {
            TaskInfo ti = stateinfo as TaskInfo;
            ti.hp.Process();
        }

        /// <summary>
        /// Server starting point
        /// </summary>
        public void Start()
        {
            while (true) {
                try
                {
                    // Create listening socket, queue size is 5 now.
                    _port = Convert.ToInt32(Program.Configuration["Port"]);
                    IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, _port);
                    serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    serverSocket.Bind(localEndPoint);
                    serverSocket.Listen(5);
                    _parent.Log("Server started at port " + _port + ".");
                    _threadModel = Program.Configuration["ThreadModel"];
                    _parent.Log("Thread model is " + _threadModel);
                    if (_threadModel is "Pool") {
                        _maxThread = Convert.ToInt32(Program.Configuration["ThreadPoolSize"]);
                        // https://msdn.microsoft.com/en-us/library/system.threading.threadpool.setmaxthreads(v=vs.110).aspx#Remarks
                        if (_maxThread < Environment.ProcessorCount)
                        {
                            _maxThread = Environment.ProcessorCount;
                        }
                        int minWorker, minIOC;
                        ThreadPool.GetMinThreads(out minWorker, out minIOC);
                        if (_maxThread < minWorker || _maxThread < minIOC)
                        {
                            _maxThread = (minWorker < minIOC) ? minIOC : minWorker;
                        }
                        ThreadPool.SetMaxThreads(_maxThread, _maxThread);
                        _parent.Log("Max pool size is " +_maxThread);
                    }
                    break;
                }
                catch (Exception ex)
                {
                    _parent.Log("Server started unsuccessfully.");
                    _parent.Log(ex.Message);
                }
                _port = _port + 1;
            }
            if (_threadModel is "Single") {
                MainLoopSingleThread();
            } else if(_threadModel is "Multi") {
                MainLoopMultiThread();
            } else if(_threadModel is "Pool") {
                MainLoopThreadPool();
            } else {
                _parent.Log("Server starting error: unknown thread model\n");
            }
        }

        private void MainLoopMultiThread()
        {
            while (true)
            {
                try
                {
                    // Wait for client
                    clientSocket = serverSocket.Accept();
                    // Get one, show some info
                    _parent.Log("Client accepted:" + clientSocket.RemoteEndPoint.ToString());
                    HTTPProcessor hp = new HTTPProcessor(clientSocket, _parent);
                    Thread thread = new Thread(new ThreadStart(hp.Process));
                    id++;
                    thread.Start();
                }
                catch (Exception ex)
                {
                    _parent.Log("Server starting error: " + ex.Message + "\n" + ex.StackTrace);
                }
            }
        }

        private void MainLoopThreadPool()
        {
            while (true)
            {
                try
                {
                    // Wait for client
                    clientSocket = serverSocket.Accept();
                    // Get one, show some info
                    _parent.Log("Client accepted:" + clientSocket.RemoteEndPoint.ToString());
                    HTTPProcessor hp = new HTTPProcessor(clientSocket, _parent);
                    TaskInfo ti = new TaskInfo(hp);
                    ThreadPool.QueueUserWorkItem(ThreadProc, ti);
                }
                catch (Exception ex)
                {
                    _parent.Log("Server starting error: " + ex.Message + "\n" + ex.StackTrace);
                }
            }
        }

        private void MainLoopSingleThread()
        {
            while (true)
            {
                try
                {
                    // Wait for client
                    clientSocket = serverSocket.Accept();
                    // Get one, show some info
                    _parent.Log("Client accepted:" + clientSocket.RemoteEndPoint.ToString());
                    HTTPProcessor hp = new HTTPProcessor(clientSocket, _parent);
                    hp.Process();
                }
                catch (Exception ex)
                {
                    _parent.Log("Server starting error: " + ex.Message + "\n" + ex.StackTrace);
                }
            }
        }
    }
}
