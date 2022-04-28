using System;
using System.Security.Principal;
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
            IPrincipal principal,
            LogHttpClient logHttpClient,
            Func<IHttpContextAccessor> httpContextAccessor
            
        )
        {
            
            return loggerConfiguration.Sink(new LogAppender(logHttpClient, principal, httpContextAccessor));
        }
    }
}
