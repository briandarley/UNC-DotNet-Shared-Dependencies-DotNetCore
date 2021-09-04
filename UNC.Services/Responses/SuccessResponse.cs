using UNC.Services.Interfaces.Response;

namespace UNC.Services.Responses
{
    public class SuccessResponse: ISuccessResponse
    {
        public string Message { get; set; }

        public SuccessResponse()
        {
            
        }

        public SuccessResponse(string message)
        {
            Message = message;
        }
    }
}
