using System;

namespace UNC.HttpClient.Exceptions
{
    public class RequestException:Exception
    {
        public string RequestUrl { get; set; }
        public RequestException(string requestUrl, string message, Exception ex) : base(message, ex)
        {
            RequestUrl = requestUrl;
        }

        
    }
}
