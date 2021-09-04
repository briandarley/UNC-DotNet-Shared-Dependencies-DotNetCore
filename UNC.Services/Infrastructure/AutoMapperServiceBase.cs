using AutoMapper;

namespace UNC.Services.Infrastructure
{
    public abstract class AutoMapperServiceBase:IAutoMapperService
    {
        

        protected void RegisterMappings()
        {
            if (IAutoMapperService.Mapper != null) return;

            IAutoMapperService.Mapper = GetMapper();
        }
        protected abstract IMapper GetMapper();
        //{
        //    var config = new MapperConfiguration(cfg => { });
        //    return config.CreateMapper();
        //}
    }
}
