using System.Collections.Generic;
using System.Linq;
using UNC.Services.Interfaces.Response;

namespace UNC.Services.Responses
{
    public class CollectionResponse<T>: ICollectionResponse<T>
    {
        public IEnumerable<T> Entities { get; set; }

        public CollectionResponse(IEnumerable<T> collection)
        {
            Entities = collection ?? Enumerable.Empty<T>();
        }

        public static CollectionResponse<T> Response(IEnumerable<T> collection)
        {
            return new CollectionResponse<T>(collection);
        }
    }
}
