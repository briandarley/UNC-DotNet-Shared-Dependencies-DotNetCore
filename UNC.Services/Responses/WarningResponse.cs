using UNC.Services.Interfaces.Response;

namespace UNC.Services.Responses
{
    public class WarningResponse: MessageResponse,IWarningResponse
    {
        public WarningResponse()
        {
            
        }
        public WarningResponse(string message)
        {
            Message = message;
        }
    }
}
