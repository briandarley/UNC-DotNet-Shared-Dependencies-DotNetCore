using System;
using UNC.Services.Interfaces.Response;

namespace UNC.Services.Responses
{
    public class ExceptionResponse: MessageResponse, IExceptionResponse,IErrorResponse
    {
        public Exception Exception { get; set; }

        public ExceptionResponse()
        {
            
        }

        public ExceptionResponse(string message)
        {
            Message = message;
        }

        public ExceptionResponse(string message, Exception exception)
        {
            Message = message;
            Exception = exception;
        }
    }
}
