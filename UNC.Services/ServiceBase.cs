using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using Serilog;
using Serilog.Events;
using UNC.Extensions.General;
using UNC.LogHandler.Models;
using UNC.Services.Interfaces;
using UNC.Services.Interfaces.Response;
using UNC.Services.Models;
using UNC.Services.Responses;

namespace UNC.Services
{
    public abstract class ServiceBase
    {
        private readonly ILogger _logger;
        private readonly IPrincipal _principal;


        private readonly RequestHeader _requestHeader;
        protected ServiceBase(ILogger logger)
        {
            _logger = logger;
        }
        protected ServiceBase(ILogger logger,IPrincipal principal = null, RequestHeader requestHeader = null)
        {
            _logger = logger;
            _principal = principal;
            _requestHeader = requestHeader;
        }


        protected bool IsAuthenticated()
        {
            return _principal?.Identity?.IsAuthenticated ?? false;
        }
        protected bool IsInRoles(IEnumerable<string> roles)
        {
            if (!IsAuthenticated()) return false;
            return roles.Any(IsInRole);
        }
        protected bool IsInRole(string role)
        {
            if (!IsAuthenticated()) return false;
            return _principal.IsInRole(role);
        }
        protected string GetClaimValue(string type)
        {
            if (!IsAuthenticated()) return string.Empty;

            var claimsPrincipal = ((ClaimsPrincipal)_principal);

            if (claimsPrincipal?.Claims is null || !claimsPrincipal.Claims.Any()) return string.Empty;

            if (claimsPrincipal.Claims.All(c => !c.Type.EqualsIgnoreCase(type))) return string.Empty;

            return claimsPrincipal.Claims.Single(c => c.Type.EqualsIgnoreCase(type)).Value;
            

        }

        protected string AuthUser()
        {
            var userName = _principal.UserName();
            if (userName.HasValue())
            {
                return userName;
            }


            if (_requestHeader == null) return "Anonymous";

            return !string.IsNullOrEmpty(_requestHeader.AuthUser)
                ? _requestHeader.AuthUser
                : null;
        }

        private LogActivityMessage InitializeLogEntry(string callerName, string sourcePath, int lineNumber)
        {



            int? ln = null;
            if (lineNumber > 0)
            {
                ln = lineNumber;
            }
            var logEntry = new LogActivityMessage
            {
                //AppSource = AppName(),
                AuthUser = AuthUser(),
                Method = callerName,
                FilePath = sourcePath,
                LineNumber = ln

            };
            return logEntry;
        }
        protected virtual void LogBeginRequest(
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string sourcePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            var logEntry = InitializeLogEntry(callerName, sourcePath, sourceLineNumber);
            logEntry.Level = LogEventLevel.Information.ToString();
            logEntry.Message = "Begin Request";

            _logger.Information(logEntry.ToJson());

        }
        protected virtual void LogEndRequest(TimeSpan? elapsed = null, [CallerMemberName] string callerName = "", [CallerFilePath] string sourcePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {

            var logEntry = InitializeLogEntry(callerName, sourcePath, sourceLineNumber);
            logEntry.Level = LogEventLevel.Information.ToString();
            logEntry.Message = "End Request";
            logEntry.Elapsed = elapsed;

            _logger.Information(logEntry.ToJson());

        }
        protected IErrorResponse LogWarning(string message, [CallerMemberName] string callerName = "", [CallerFilePath] string sourcePath = "", [CallerLineNumber] int sourceLineNumber = 0, bool includeFullTrace = true)
        {
            var logEntry = InitializeLogEntry(callerName, sourcePath, sourceLineNumber);
            logEntry.Level = LogEventLevel.Warning.ToString();

            var sb = new StringBuilder(message);


            if (includeFullTrace && FullTrace().ToList().Count > 0)
            {
                sb.AppendLine("Full trace follows...");
                sb.AppendLine($"Trace: {string.Join(",", FullTrace().ToList())}");
            }

            logEntry.Message = sb.ToString();

            _logger.Warning(logEntry.ToJson());

            var warningMessage = $"{message}: {callerName}";
            var response = new WarningResponse(warningMessage);

            return response;
        }
        protected IErrorResponse LogError(string message, [CallerMemberName] string callerName = "", [CallerFilePath] string sourcePath = "", [CallerLineNumber] int sourceLineNumber = 0, bool includeFullTrace = true)
        {
            var logEntry = InitializeLogEntry(callerName, sourcePath, sourceLineNumber);
            logEntry.Level = LogEventLevel.Error.ToString();
            

            var sb = new StringBuilder(message);


            if (includeFullTrace && FullTrace().ToList().Count > 0)
            {
                sb.AppendLine("Full trace follows...");
                sb.AppendLine($"Trace: {string.Join(",", FullTrace().ToList())}");
            }

            logEntry.Message = sb.ToString();

            _logger.Error(logEntry.ToJson());

            var errorMessage = $"{message}: {callerName}";
            var response = new ErrorResponse(errorMessage);

            return response;
        }
        protected IInfoResponse LogInfo(string message, [CallerMemberName] string callerName = "", [CallerFilePath] string sourcePath = "", [CallerLineNumber] int sourceLineNumber = 0, bool includeFullTrace = false)
        {
            var logEntry = InitializeLogEntry(callerName, sourcePath, sourceLineNumber);
            logEntry.Level = LogEventLevel.Information.ToString();

            var sb = new StringBuilder(message);


            if (includeFullTrace && FullTrace().ToList().Count > 0)
            {
                sb.AppendLine("Full trace follows...");
                sb.AppendLine($"Trace: {string.Join(",", FullTrace().ToList())}");
            }

            logEntry.Message = sb.ToString();

            _logger.Information(logEntry.ToJson());

            var infoMessage = $"{message}: {callerName}";
            var response = new InfoResponse(infoMessage);

            return response;

        }

        protected IDebugResponse LogDebug(string message,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string sourcePath = "",
            [CallerLineNumber] int sourceLineNumber = 0,
            bool includeFullTrace = false)
        {
            var logEntry = InitializeLogEntry(callerName, sourcePath, sourceLineNumber);
            logEntry.Level = LogEventLevel.Debug.ToString();

            var sb = new StringBuilder(message);


            if (includeFullTrace && FullTrace().ToList().Count > 0)
            {
                sb.AppendLine("Full trace follows...");
                sb.AppendLine($"Trace: {string.Join(",", FullTrace().ToList())}");
            }

            logEntry.Message = sb.ToString();

            _logger.Debug(logEntry.ToJson());

            var debugMessage = $"{message}: {callerName}";
            var response = new DebugResponse(debugMessage);

            return response;
        }

        protected IExceptionResponse LogException(
            Exception ex,
            bool throwException,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string sourcePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            var logEntry = InitializeLogEntry(callerName, sourcePath, sourceLineNumber);
            logEntry.Level = LogEventLevel.Error.ToString();

            var sb = new StringBuilder($"Unexpected Error calling {callerName}. Exception Type: {ex.GetType()}. ");
            sb.Append(ex.Message);

            if (ex.InnerException != null)
            {
                sb.AppendLine("Inner Exception: ");
                sb.AppendLine(ex.InnerException.Message);
            }


            if (FullTrace().ToList().Count > 0)
            {
                sb.Append(" Full trace follows...");
                sb.AppendLine($"Trace: {string.Join(",", FullTrace().ToList())}");
            }

            logEntry.Message = sb.ToString();

            _logger.Error(logEntry.ToJson());

            if (throwException)
            {
                throw ex;
            }


            var errorMessage = $"{ex.Message}: {callerName}";
            return new ExceptionResponse(errorMessage) { Exception = ex };

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

        private string GetLogDetailsFromRequestHeaders()
        {
            if (_requestHeader == null) return string.Empty;

            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(_requestHeader.AuthUser))
            {
                sb.Append($", Calling Auth User: {_requestHeader.AuthUser}");
            }

            if (!string.IsNullOrEmpty(_requestHeader.ApplicationName))
            {
                sb.Append($", Calling Application Name: {_requestHeader.ApplicationName}");
            }

            return sb.ToString();

        }

        protected SuccessResponse SuccessResponse(string message = "")
        {
            return new SuccessResponse(message);
        }
        protected virtual NoWorkPerformedResponse NoWorkPerformedResponse(string message = "")
        {
            return new NoWorkPerformedResponse(message);
        }
        protected virtual IResponse ErrorResponse(string error)
        {
            return new ErrorResponse(error);
        }

        protected virtual IResponse CollectionResponse<T>(IEnumerable<T> values)
        {
            return new CollectionResponse<T>(values);
        }

        protected virtual IResponse TypedResponse<T>(T value)
        {
            return new TypedResponse<T>(value);
        }

        protected virtual IResponse NotFoundResponse(string message = "")
        {
            return new NotFoundResponse(message);
        }

        protected virtual IResponse ErrorResponse(IEnumerable<ValidationResult> validationResult)
        {
            var msg = new
            {
                Message = "Model Validation Error",
                ValidationResult = validationResult.ToList()

            };
            return new ErrorResponse(msg.ToJson());


        }

        protected virtual IResponse EntityResponse<T>(T value) where T : IEntity
        {
            return new EntityResponse<T>(value);
        }

        protected IEnumerable<ValidationResult> ValidateModel<T>(T model, out bool isValid) where T : class
        {
            var validationResultList = new List<ValidationResult>();
            var validationContext = new ValidationContext(model);
            isValid = Validator.TryValidateObject(model, validationContext, validationResultList, true);
            return validationResultList;
        }
        protected bool IsValidModel<T>(T model) where T : class
        {
            var validationResultList = new List<ValidationResult>();
            var validationContext = new ValidationContext(model);
            return Validator.TryValidateObject(model, validationContext, validationResultList, true);
        }

        protected bool IsValidModel<T>(IEnumerable<T> model) where T : class
        {
            var validationResultList = new List<ValidationResult>();
            var validationContext = new ValidationContext(model);
            return Validator.TryValidateObject(model, validationContext, validationResultList, true);
        }



    }
}
