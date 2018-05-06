using System;
using System.Collections.Generic;
using System.Text;
using ServiceStack.Redis;

namespace DNWS
{
    class StatPlugin : IPlugin
    {
        protected static Dictionary<String, int> statDictionary = null;
        protected static RedisManagerPool redisManager = null;
        public StatPlugin()
        {
            string useRedis = Environment.GetEnvironmentVariable("redis");

            if (useRedis != null)
            {
                if (redisManager == null) {
                  Console.WriteLine("Using Redis");
                  redisManager = new RedisManagerPool("redis:6379");
                }
            }
            else if (statDictionary == null)
            {
                statDictionary = new Dictionary<String, int>();
            }
        }

        public void PreProcessing(HTTPRequest request)
        {
            if (redisManager != null)
            {
                using (var client = redisManager.GetClient())
                {
                    client.IncrementValue(request.Url);
                }
            }
            else if (statDictionary.ContainsKey(request.Url))
            {
                statDictionary[request.Url] = (int)statDictionary[request.Url] + 1;
            }
            else
            {
                statDictionary[request.Url] = 1;
            }
        }
        public virtual HTTPResponse GetResponse(HTTPRequest request)
        {
            HTTPResponse response = null;
            StringBuilder sb = new StringBuilder();
            sb.Append("<html><body><h1>Stat:</h1>");
            if (redisManager != null) {
              using (var client = redisManager.GetClient()) {
                List<String> keys = client.GetAllKeys();
                foreach (String key in keys)
                {
                    sb.Append(key + ": " + client.GetValue(key) + "<br />");
                }
              }
            }
            else
            {
              foreach (KeyValuePair<String, int> entry in statDictionary)
              {
                  sb.Append(entry.Key + ": " + entry.Value.ToString() + "<br />");
              }
            }
            sb.Append("</body></html>");
            response = new HTTPResponse(200);
            response.Body = Encoding.UTF8.GetBytes(sb.ToString());
            return response;
        }

        public HTTPResponse PostProcessing(HTTPResponse response)
        {
            throw new NotImplementedException();
        }
    }
}