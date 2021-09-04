using AutoMapper;

namespace UNC.Services.Infrastructure
{
    /// <summary>
    /// This service should be set up as a singleton,
    /// The constructor should be responsible for registering the mappings and setting the lone property Mapper
    /// </summary>
    public interface IAutoMapperService
    {

        public static IMapper Mapper;



    }
}
