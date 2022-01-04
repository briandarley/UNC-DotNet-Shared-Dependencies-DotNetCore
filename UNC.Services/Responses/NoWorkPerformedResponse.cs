using UNC.Services.Interfaces.Response;

namespace UNC.Services.Responses
{
    public class NoWorkPerformedResponse : MessageResponse, ISuccessResponse
    {
        public NoWorkPerformedResponse() { }
        public NoWorkPerformedResponse(string message)
        {
            Message = message;
        }
    }
}
