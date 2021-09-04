using System.Net.Http;

namespace UNC.LogHandler.Models
{
    public class LogHttpClient
    {
        public HttpClient HttpClient { get;  }

        public LogHttpClient(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }
    }
}
