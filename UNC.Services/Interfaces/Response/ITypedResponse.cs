namespace UNC.Services.Interfaces.Response
{
    public interface ITypedResponse<T>: IResponse
    {
        T Entity { get; set; }
        
    }
}
