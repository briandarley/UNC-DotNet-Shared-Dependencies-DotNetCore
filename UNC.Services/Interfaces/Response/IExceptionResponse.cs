using System;
using UNC.Services.Responses;

namespace UNC.Services.Interfaces.Response
{
    public interface IExceptionResponse:IResponse
    {
        Exception Exception { get; set; }
        string Message { get; set; }
    }
}
