using System.Collections.Generic;

namespace UNC.Services.Interfaces.Response
{
    public interface ICollectionResponse : IResponse
    {

    }
    public interface ICollectionResponse<T>: ICollectionResponse
    {
        IEnumerable<T> Entities { get; set; }
    }
}
