using System;

namespace UNC.HttpClient.EventHandlers
{
    public class WebRequestException : EventArgs
    {
        public string Path { get; set; }
        public object Entity { get; set; }
        public Exception Exception { get; set; }
        public bool LogError { get; set; }
        public bool ExceptionAcknowledged { get; set; }
        public string Action { get; set; }
        public string Message { get; set; }
    }
}
