using System;
using System.Runtime.CompilerServices;
using UNC.Services.Interfaces.Response;

namespace UNC.Services
{
    public interface ILogService
    {
        void LogBeginRequest([CallerMemberName] string callerName = "", [CallerFilePath] string sourcePath = "", [CallerLineNumber] int sourceLineNumber = 0, string pathUri = "");
        void LogEndRequest(TimeSpan? elapsed = null, [CallerMemberName] string callerName = "", [CallerFilePath] string sourcePath = "", [CallerLineNumber] int sourceLineNumber = 0, string pathUri = "");


        IDebugResponse LogDebug(string message, [CallerMemberName] string callerName = "", [CallerFilePath] string sourcePath = "", [CallerLineNumber] int sourceLineNumber = 0, bool includeFullTrace = false, string pathUri = "");
        IInfoResponse LogInfo(string message, [CallerMemberName] string callerName = "", [CallerFilePath] string sourcePath = "", [CallerLineNumber] int sourceLineNumber = 0, bool includeFullTrace = false, string pathUri = "");
        IWarningResponse LogWarning(string message, [CallerMemberName] string callerName = "", [CallerFilePath] string sourcePath = "", [CallerLineNumber] int sourceLineNumber = 0, bool includeFullTrace = true, string pathUri = "");
        IErrorResponse LogError(string message, [CallerMemberName] string callerName = "", [CallerFilePath] string sourcePath = "", [CallerLineNumber] int sourceLineNumber = 0, bool includeFullTrace = false, string pathUri = "");
        IExceptionResponse LogException(Exception ex, bool throwException, [CallerMemberName] string callerName = "", [CallerFilePath] string sourcePath = "", [CallerLineNumber] int sourceLineNumber = 0, string pathUri = "");
    }
}
