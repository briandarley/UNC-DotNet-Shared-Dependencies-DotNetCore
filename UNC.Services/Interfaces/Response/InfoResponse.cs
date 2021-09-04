namespace UNC.Services.Interfaces.Response
{
    public class InfoResponse: IInfoResponse
    {
        public string Message { get; set; }

        public InfoResponse()
        {
            
        }

        public InfoResponse(string message)
        {
            Message = message;
        }
    }
}
