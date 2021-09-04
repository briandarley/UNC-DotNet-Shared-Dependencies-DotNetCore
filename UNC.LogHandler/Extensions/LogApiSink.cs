using System;
using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Configuration;
using UNC.LogHandler.Models;

namespace UNC.LogHandler.Extensions
{
    public static class LogApiSink
    {
       

        public static LoggerConfiguration LogAppender(
            this LoggerSinkConfiguration loggerConfiguration,
            LogHttpClient logHttpClient,
            Func<IHttpContextAccessor> httpContextAccessor,
            IFormatProvider fmtProvider = null
        )
        {
            
            return loggerConfiguration.Sink(new LogAppender(logHttpClient, httpContextAccessor));
        }
    }
}
