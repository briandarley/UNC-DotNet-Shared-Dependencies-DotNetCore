using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerUI;
using UNC.Extensions.General;
using UNC.Services.Infrastructure;

namespace UNC.API.Base.Infrastructure
{
    /// <summary>
    /// Some info on middleware
    /// https://stackoverflow.com/questions/32459670/resolving-instances-with-asp-net-core-di-from-within-configureservices
    /// 
    /// </summary>
    public static class ApiMiddlewareRegistrar
    {
        /// <summary>
        /// Not Middleware, but we want to provide some abstraction and friendly initialization for AutoMapperService
        /// Since it's a singleton, we only need to call this once and the constructor will initialize everything for us
        /// </summary>
        /// <param name="app"></param>
        public static void UseUncInitializeAutoMapper(this IApplicationBuilder app)
        {
            app.ApplicationServices.GetService<IAutoMapperService>();
        }
        /// <summary>
        /// Not Middleware, Performing more abstraction for initializing swagger. 
        /// </summary>
        /// <param name="app"></param>
        /// <param name="applicationTitle">If empty, will retrieve from configuration file, parameter 'Application'</param>
        public static void UseUncRegisterSwagger(this IApplicationBuilder app, string applicationTitle = "")
        {
            if (applicationTitle.IsEmpty())
            {
                var configuration = app.ApplicationServices.GetService<IConfiguration>();
                applicationTitle = configuration.GetValue<string>("Application");
            }


            app.UseSwagger((Action<SwaggerOptions>)null);
            app.UseSwaggerUI((Action<SwaggerUIOptions>)(c => c.SwaggerEndpoint("../swagger/v1/swagger.json", applicationTitle)));



        }

    }
}
