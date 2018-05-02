using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace DNWS
{
  // We use all the functionality from StatPlugin (so we inherit from it), just reimplement 
  // how we will response to client;

  class StatAPIPlugin : StatPlugin, IPlugin
  {

    //Response model, this make it easier to shape the output
    public class StatusResponse
    {
      private string _url;
      private int _count;
      public string Url {
        get
        {
            return _url;
        }
        set
        {
          if(value == null || value == "") {
            return;
          }
          _url = value;
        }
      }
      public int Count {
        get
        {
          return _count;
        }
        set
        {
          _count = (value < 0)? 0 : value;
        }
      }

      public StatusResponse(string url, int count)
      {
        Url = url;
        Count = count;
      }
    }

    public override HTTPResponse GetResponse(HTTPRequest request)
    {
      // Check the request method first.
      if(request.Method == "GET") {
        HTTPResponse response = null;
        // Create new list of response model ,this depend on output format;
        List<StatusResponse> responseList = new List<StatusResponse>();
        // Fill in  response model list
        foreach (KeyValuePair<String, int> entry in statDictionary)
        {
          responseList.Add(new StatusResponse(entry.Key, entry.Value));
        }
        // Set response status and type
        response = new HTTPResponse(200);
        response.Type = "application/json";
        // Convert response model into json string, then byte array;
        string resp = JsonConvert.SerializeObject(responseList);
        response.Body = Encoding.UTF8.GetBytes(resp);
        return response;
      }
      return new HTTPResponse(501);
    }
  }
}