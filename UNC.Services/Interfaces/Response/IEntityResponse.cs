namespace UNC.Services.Interfaces.Response
{
    public interface IEntityResponse : IResponse
    {
        
    }
    public interface IEntityResponse<TEntity> : IEntityResponse where TEntity : IEntity
    {
        TEntity Entity { get; set; }

        
    }
}
