using UNC.Services.Interfaces.Response;

namespace UNC.Services.Responses
{
    public class TypedResponse
    {
        public static TypedResponse<T> Response<T>(T value)
        {
            return new TypedResponse<T>(value);
        }
    }
    public class TypedResponse<T>:ITypedResponse<T>
    {
        public T Entity { get; set; }

        public TypedResponse()
        {
            
        }

        public TypedResponse(T entity)
        {
            Entity = entity;
        }

      
    }
}
