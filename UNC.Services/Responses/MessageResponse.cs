using UNC.Services.Interfaces.Response;

namespace UNC.Services.Responses
{
    public abstract class MessageResponse : IMessageResponse
    {
        public string Message { get; set; }
    }
}
