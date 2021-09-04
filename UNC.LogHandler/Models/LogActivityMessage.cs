using System;
using System.Text.Json;

namespace UNC.LogHandler.Models
{
    public class LogActivityMessage
    {
        public string PathUri { get; set; }
        public string Method { get; set; }
        public string Application { get; set; }
        public string ServiceAccount { get; set; }

        public string AuthUser { get; set; }
        public string AppSource { get; set; }
        public string Message { get; set; }
        public string FilePath { get; set; }
        public int? LineNumber { get; set; }
        public string  ThreadId { get; set; }
        public string Level { get; set; }
        public TimeSpan? Elapsed { get; set; }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
