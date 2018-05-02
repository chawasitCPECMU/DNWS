using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNWS
{
    // RFC1945
    public class HTTPResponse
    {
        protected int _status = 404;
        public int Status
        {
            get { return _status; }
            set { _status = value; }
        }

        protected byte[] _body;
        public byte[] Body
        {
            get { return _body; }
            set { _body = value; }
        }


        protected string _type = "text/html";
        public string Type
        {
            get { return _type; }
            set { _type = value; }
        }

        Dictionary<string, string> _customHeader = new Dictionary<string, string>();

        public Dictionary<string, string> CustomHeader 
        {
            get
            {
                return _customHeader;
            } 
            set
            {
                _customHeader = value;
            }
        }

        public bool AddCustomHeader(string key, string value)
        {
            if(_customHeader.ContainsKey(key)) {
                return false;
            }
            _customHeader.Add(key, value);
            return true;
        }
        public void SetBody(string msg)
        {
            Body = Encoding.UTF8.GetBytes(msg);
        }

        public String Header
        {
            get
            {
                StringBuilder headerResponse = new StringBuilder("HTTP/1.0 ");
                switch (_status)
                {
                    case 200:
                        headerResponse.Append("200 OK");
                        break;
                    case 201:
                        headerResponse.Append("201 Created");
                        break;
                    case 301:
                        headerResponse.Append("301 Moved Permanently");
                        break;
                    case 400:
                        headerResponse.Append("400 Bad Request");
                        break;
                    case 403:
                        headerResponse.Append("403 Forbidden");
                        break;
                    case 404:
                        headerResponse.Append("404 Not Found");
                        break;
                    case 500:
                        headerResponse.Append("500 Internal Server Error");
                        break;
                    case 501:
                        headerResponse.Append("501 Not Implemented");
                        break;
                }

                headerResponse.Append("\r\n");
                headerResponse.Append("Content-Type: ").Append(Type).Append("\r\n");
                headerResponse.Append("Connection: close\r\n");
                headerResponse.Append("Server: DNWS 1.0\r\n");
                headerResponse.Append("Access-Control-Allow-Origin: *\r\n");
                headerResponse.Append("Access-Control-Allow-Headers: Content-Type, X-session \r\n");
                headerResponse.Append("Access-Control-Allow-Methods: GET, POST, OPTIONS, DELETE\r\n");
               foreach(KeyValuePair<string, string> entry in _customHeader) {
                    headerResponse.Append(entry.Key).Append(": ").Append(entry.Value).Append("\r\n");
                }
                headerResponse.Append("\r\n");
                return headerResponse.ToString();
            }
        }

        public HTTPResponse(int status)
        {
            _status = status;
        }

    }
}
