using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;
using UNC.LogHandler.Models;
using UNC.Services.Interfaces.Response;
using UNC.Services.Models;
using UNC.Services.Responses;

namespace UNC.Services
{
    /// <summary>
    /// Be sure to never use singleton, requestheader will likely change for each request
    /// </summary>
    public class LogService : ILogService
    {
        private readonly ILogger _logger;
        private readonly IPrincipal _principal;

        private readonly RequestHeader _requestHeader;
        private static string _applicationName;
        public LogService(ILogger logger,  IConfiguration configuration, IPrincipal principal = null, RequestHeader requestHeader = null)
        {
            _logger = logger;
            _principal = principal;
            _requestHeader = requestHeader;

            if (string.IsNullOrEmpty(_applicationName))
            {
                _applicationName = configuration.GetSection("Application").Value;
            }
            
        }

        protected string AppName()
        {
            if (_requestHeader == null) return null;

            return !string.IsNullOrEmpty(_requestHeader.ApplicationName) ? _requestHeader.ApplicationName : _applicationName;
        }

        protected string AuthUser()
        {
            if (_principal?.Identity?.Name != null)
            {
                return _principal.Identity.Name;
            }

            var claimsPrincipal = (ClaimsPrincipal)_principal;

            if (claimsPrincipal?.Claims != null)
            {
                var sub = claimsPrincipal.Claims.FirstOrDefault(c => c.Type.Equals("sub"));

                if (sub?.Value != null)
                {
                    return sub.Value;
                }
            }

            

            if (_requestHeader == null) return null;

            return !string.IsNullOrEmpty(_requestHeader.AuthUser)
                ? _requestHeader.AuthUser
                : null;
        }

        private LogActivityMessage InitializeActivityMessage(string callerName, string sourcePath, int lineNumber, string pathUri)
        {
            int? ln = null;
            if (lineNumber > 0)
            {
                ln = lineNumber;
            }
            var message = new LogActivityMessage
            {
                AppSource = AppName(),
                ServiceAccount = Environment.UserName,
                AuthUser = AuthUser(),
                Method = callerName,
                FilePath = sourcePath,
                LineNumber = ln,
                PathUri =  pathUri

            };
            return message;
        }
        private IEnumerable<string> FullTrace()
        {
            var index = 0;
            var stackTrace = new StackTrace(true);

            var frames = stackTrace.GetFrames();
            if (frames == null || !frames.Any())
            {
                yield break;
            }
            foreach (var r in frames)
            {
                if (index == 0)
                {
                    continue;
                }
                index++;
                yield return $"Filename: {r.GetFileName()} Method: {r.GetMethod()} Line: {r.GetFileLineNumber()} Column: {r.GetFileColumnNumber()}  ";
            }

        }
        public void LogBeginRequest(string callerName = "", string sourcePath = "", int sourceLineNumber = 0, string pathUri = "")
        {
            var activityMessage = InitializeActivityMessage(callerName, sourcePath, sourceLineNumber, pathUri);

            activityMessage.Message = "Begin Request";

            _logger.Information(activityMessage.ToString());

        }

        public void LogEndRequest(TimeSpan? elapsed = null, string callerName = "", string sourcePath = "", int sourceLineNumber = 0, string pathUri = "")
        {

            var activityMessage = InitializeActivityMessage(callerName, sourcePath, sourceLineNumber, pathUri);
            activityMessage.Message = "End Request";
            activityMessage.Elapsed = elapsed;

            _logger.Information(activityMessage.ToString());
        }

        public IDebugResponse LogDebug(string message, string callerName = "", string sourcePath = "", int sourceLineNumber = 0, bool includeFullTrace = false, string pathUri = "")
        {
            var activityMessage = InitializeActivityMessage(callerName, sourcePath, sourceLineNumber, pathUri);

            activityMessage.Message = message;

            _logger.Debug(activityMessage.ToString());

            if (includeFullTrace && FullTrace().ToList().Count > 0)
            {
                _logger.Debug($"Trace: {string.Join(",", FullTrace().ToList())}");
            }

            var debugMessage = $"{message}: {callerName}";
            var response = new DebugResponse(debugMessage);

            return response;
        }

        public IInfoResponse LogInfo(string message, string callerName = "", string sourcePath = "", int sourceLineNumber = 0, bool includeFullTrace = false, string pathUri = "")
        {
            var activityMessage = InitializeActivityMessage(callerName, sourcePath, sourceLineNumber, pathUri);

            activityMessage.Message = message;

            _logger.Information(activityMessage.ToString());

            if (includeFullTrace && FullTrace().ToList().Count > 0)
            {
                _logger.Information($"Trace: {string.Join(",", FullTrace().ToList())}");
            }

            var infoMessage = $"{message}: {callerName}";

            var response = new InfoResponse(infoMessage);

            return response;
        }

        public IWarningResponse LogWarning(string message, string callerName = "", string sourcePath = "", int sourceLineNumber = 0, bool includeFullTrace = false, string pathUri = "")
        {
            var activityMessage = InitializeActivityMessage(callerName, sourcePath, sourceLineNumber, pathUri);

            activityMessage.Message = message;

            _logger.Warning(activityMessage.ToString());

            if (includeFullTrace && FullTrace().ToList().Count > 0)
            {
                _logger.Warning($"Trace: {string.Join(",", FullTrace().ToList())}");
            }

            var warningMessage = $"{message}: {callerName}";

            var response = new WarningResponse(warningMessage);

            return response;
        }

        public IErrorResponse LogError(string message, string callerName = "", string sourcePath = "", int sourceLineNumber = 0, bool includeFullTrace = false, string pathUri = "")
        {
            var activityMessage = InitializeActivityMessage(callerName, sourcePath, sourceLineNumber, pathUri);

            activityMessage.Message = message;

            _logger.Error(activityMessage.ToString());

            if (includeFullTrace && FullTrace().ToList().Count > 0)
            {
                _logger.Error($"Trace: {string.Join(",", FullTrace().ToList())}");
            }

            var errorMessage = $"{message}: {callerName}";

            var response = new ErrorResponse(errorMessage);

            return response;
        }


        public IExceptionResponse LogException(Exception ex, bool throwException, string callerName = "", string sourcePath = "", int sourceLineNumber = 0, string pathUri = "")
        {
            var activityMessage = InitializeActivityMessage(callerName, sourcePath, sourceLineNumber, pathUri);

            var sbError = new StringBuilder();
            sbError.AppendLine(ex.Message);

            if (ex.InnerException != null)
            {
                sbError.AppendLine("Inner Exception: ");
                sbError.AppendLine(ex.InnerException.Message);
            }

            activityMessage.Message = sbError.ToString();

            var exception = ex.InnerException ?? ex;

            _logger.Error(exception, activityMessage.ToString());


            if (throwException)
            {
                throw ex;
            }
            var response = new ExceptionResponse($"Unexpected Error calling {callerName}", ex);

            return response;
        }
    }
}
