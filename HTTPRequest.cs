using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DNWS
{
  public class HTTPRequest
  {
    public const string METHOD_POST = "POST";
    public const string METHOD_GET = "GET";
    public const string METHOD_DELETE = "DELETE";
    public const string METHOD_OPTIONS = "OPTIONS";
    protected String _url;
    protected String _filename;
    protected Dictionary<String, String> _propertyListDictionary = null;
    protected Dictionary<String, String> _requestListDictionary = null;

    protected String _body;

    protected int _status;

    protected String _method;

    public String Url
    {
      get { return _url;}
    }

    public String Filename
    {
      get { return _filename;}
    }

    public String Body
    {
      get {return _body;}
    }

    public int Status
    {
      get {return _status;}
    }

    public String Method
    {
      get {return _method;}
    }
    public HTTPRequest(String request)
    {
      _propertyListDictionary = new Dictionary<String, String>();
      String[] lines = Regex.Split(request, "\\n");

      if(lines.Length == 0) {
        _status = 500;
        return;
      }

      String[] statusLine = Regex.Split(lines[0], "\\s");
      if(statusLine.Length != 4) { // too short something is wrong
        _status = 401;
        return;
      }
      if (statusLine[0].ToLower().Equals("get"))
      {
        _method = METHOD_GET;
      } else if(statusLine[0].ToLower().Equals("post")) {
        _method = METHOD_POST;
      } else if(statusLine[0].ToLower().Equals("delete")) {
        _method = METHOD_DELETE;
      } else if(statusLine[0].ToLower().Equals("options")) {
        _method = METHOD_OPTIONS;
      } else {
        _status = 501;
        return;
      }
      _status = 200;

      _url = statusLine[1];
      String[] urls = Regex.Split(_url, "/");
      _filename = urls[urls.Length - 1];
      String[] parts = Regex.Split(_filename, "[?]");
      if (parts.Length > 1 && parts[1].Contains('&'))
      {
        //Ref: http://stackoverflow.com/a/4982122
        _requestListDictionary = parts[1].Split('&').Select(x => x.Split('=')).ToDictionary(x => x[0].ToLower(), x => x[1]);
      } else{
        _requestListDictionary = new Dictionary<String, String>();
        if (parts.Length > 1) {
          String[] requestParts = Regex.Split(parts[1], "[=]");
          if(requestParts.Length > 1) {
            AddRequest(requestParts[0], requestParts[1]);
          }
        }
      }

      if(lines.Length == 1) return;

      for(int i = 1; i != lines.Length; i++) {
        String[] pair = Regex.Split(lines[i], ": "); //FIXME
        if(pair.Length == 0) continue;
        if(pair.Length == 1) { // handle post body
          if(pair[0].Length > 1) { //FIXME, this is a quick hack
            Dictionary<String, String> _bodys = pair[0].Split('&').Select(x => x.Split('=')).ToDictionary(x => x[0].ToLower(), x => x[1]);
            foreach(KeyValuePair<string, string> entry in _bodys) {
              if(!_requestListDictionary.ContainsKey(entry.Key)) {
                AddRequest(entry.Key, entry.Value);
              }
            }
          }
        } else { // Length == 2, GET url request
          AddProperty(pair[0], pair[1]);
        }
      }
    }
    public String GetPropertyByKey(String key)
    {
      if(_propertyListDictionary.ContainsKey(key.ToLower())) {
        return _propertyListDictionary[key.ToLower()];
      } else {
        return null;
      }
    }
    public String GetRequestByKey(String key)
    {
      if(_requestListDictionary.ContainsKey(key.ToLower())) {
        return _requestListDictionary[key.ToLower()];
      } else {
        return null;
      }
    }

    public void AddProperty(String key, String value)
    {
      _propertyListDictionary.Add(key.ToLower(), value.TrimEnd('\r', '\n'));
    }
    public void AddRequest(String key, String value)
    {
      _requestListDictionary.Add(key.ToLower(), value.TrimEnd('\r', '\n'));
    }
  }
}