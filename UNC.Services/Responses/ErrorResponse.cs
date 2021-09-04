using UNC.Services.Interfaces.Response;

namespace UNC.Services.Responses
{
    public class ErrorResponse: MessageResponse,IErrorResponse
    {
        public ErrorResponse() { }
        public ErrorResponse(string message)
        {
            Message = message;
        }
    }
}
