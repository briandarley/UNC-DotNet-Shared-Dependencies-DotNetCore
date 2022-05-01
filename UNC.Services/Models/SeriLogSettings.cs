using Serilog.Events;

namespace UNC.Services.Models
{
    public class SeriLogSettings
    {
        public string ApplicationName { get; set; }
        public string LogFilePath { get; set; }

        public LogEventLevel LogEventLevel { get; set; }

        public string OutputTemplate { get; set; }


    }
}
