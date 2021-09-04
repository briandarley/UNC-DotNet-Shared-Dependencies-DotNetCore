using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using UNC.API.Base.Infrastructure;
using UNC.API.Base.Models;
using UNC.Services;
using RequestHeaderValues = UNC.API.Base.Constants.RequestHeaderConstants.CodeValueConstants;
namespace UNC.API.Base
{
    public abstract class BaseController:Controller
    {
        private readonly ILogService _logService;
        private readonly IPrincipal _principal;
        private RequestHeaderModel _requestHeaders;

        public string UserId => User.UserId();


        protected BaseController(ILogService logService)
        {
            _logService = logService;
            
        }

        protected BaseController(ILogService logService, IPrincipal principal)
        {
            _logService = logService;
            _principal = principal;
        }

        private RequestHeaderModel GetRequestHeaders()
        {
            if (_requestHeaders != null)
                return _requestHeaders;
            var header1 = Request.Headers[RequestHeaderValues.APPLICATION_NAME];
            var header2 = Request.Headers[RequestHeaderValues.AUTH_USER];
            
            var response = (RequestHeaderModel)null;

            if (header1.Any())
            {
                response = new RequestHeaderModel { ApplicationName = header1[0] };
            }

            if (header2.Any())
            {
                if (response == null)
                {
                    response = new RequestHeaderModel();
                }
                response.AuthUser = header2[0];
            }

            _requestHeaders = response;

            return _requestHeaders;

        }

        protected string AppName()
        {
            var requestHeaders = GetRequestHeaders();

            if (requestHeaders == null) return null;

            return !string.IsNullOrEmpty(requestHeaders.ApplicationName) ? requestHeaders.ApplicationName : null;
        }

        protected string AuthUser()
        {
            if (_principal?.Identity.Name != null)
            {
                return _principal?.Identity.Name;
            }
            var requestHeaders = GetRequestHeaders();

            if (requestHeaders == null) return "Anonymous";

            return !string.IsNullOrEmpty(requestHeaders.AuthUser)
                ? requestHeaders.AuthUser
                : null;
        }


        protected void LogBeginRequest([CallerMemberName] string callerName = "", [CallerFilePath] string sourcePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            _logService.LogBeginRequest(callerName, sourcePath, sourceLineNumber, Request.GetDisplayUrl());
        }

        protected void LogEndRequest(TimeSpan? elapsed = null, [CallerMemberName] string callerName = "", [CallerFilePath] string sourcePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            _logService.LogEndRequest(elapsed, callerName, sourcePath, sourceLineNumber, Request.GetDisplayUrl());
        }


        protected IActionResult LogWarning(string message, [CallerMemberName] string callerName = "", [CallerFilePath] string sourcePath = "", [CallerLineNumber] int sourceLineNumber = 0, bool includeFullTrace = true)
        {
            _logService.LogWarning(message, callerName, sourcePath, sourceLineNumber, includeFullTrace, Request.GetDisplayUrl());


            return BadRequest();
        }

        protected void LogInfo(string message, [CallerMemberName] string callerName = "", [CallerFilePath] string sourcePath = "", [CallerLineNumber] int sourceLineNumber = 0, bool includeFullTrace = false)
        {
            _logService.LogInfo(message, callerName, sourcePath, sourceLineNumber, includeFullTrace, Request.GetDisplayUrl());
        }

        protected void LogDebug(string message, [CallerMemberName] string callerName = "", [CallerFilePath] string sourcePath = "", [CallerLineNumber] int sourceLineNumber = 0, bool includeFullTrace = false)
        {
            _logService.LogDebug(message, callerName, sourcePath, sourceLineNumber, includeFullTrace, pathUri: Request.GetDisplayUrl());
        }

        protected IActionResult LogException(Exception ex, bool throwException, [CallerMemberName] string callerName = "", [CallerFilePath] string sourcePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            _logService.LogException(ex, throwException, callerName, sourcePath, sourceLineNumber, pathUri: Request.GetDisplayUrl());

            return BadRequest();
        }


    }
}
