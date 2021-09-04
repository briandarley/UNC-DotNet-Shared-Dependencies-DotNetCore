using UNC.Services.Responses;

namespace UNC.Services.Interfaces.Response
{
    public interface IMessageResponse:IResponse
    {
        string Message { get; set; }
    }
}
