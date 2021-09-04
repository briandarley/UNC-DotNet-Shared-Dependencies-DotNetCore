using UNC.Services.Interfaces.Response;

namespace UNC.Services.Responses
{
    public class NotFoundResponse:MessageResponse, INotFoundResponse
    {
        public NotFoundResponse()
        {
            
        }

        public NotFoundResponse(string message)
        {
            Message = message;
        }
    }
}
