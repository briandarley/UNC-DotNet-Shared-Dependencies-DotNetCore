using UNC.Services.Interfaces.Response;

namespace UNC.Services.Responses
{
    public class DebugResponse:MessageResponse, IDebugResponse
    {
        public DebugResponse() { }

        public DebugResponse(string message)
        {
            Message = message;
        }
    }
}
