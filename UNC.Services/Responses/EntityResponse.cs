using System;
using UNC.Services.Interfaces;
using UNC.Services.Interfaces.Response;

namespace UNC.Services.Responses
{
    [Serializable]
    public class EntityResponse: IEntity
    {
        public static EntityResponse<TEntity> Response<TEntity>(TEntity entity) where TEntity : IEntity
        {
            return new EntityResponse<TEntity>(entity);
        }
    }
    [Serializable]
    public class EntityResponse<TEntity> : IEntityResponse<TEntity> where TEntity : IEntity
    {
        public TEntity Entity { get; set; }

        public EntityResponse(TEntity entity)
        {
            Entity = entity;
        }

     
    }
}
