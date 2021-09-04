using System.Security.Principal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using UNC.Services.Models;

namespace UNC.HttpClient.Extensions
{
    public static class Extensions
    {
        public static void RegisterDependencies(this IServiceCollection services)
        {
            services.AddTransient(cfg =>
            {
                var requestHeader = new RequestHeader
                {
                    ApplicationName = "",
                    Principal = cfg.GetService<IPrincipal>()
                    };
                return requestHeader;

            });
            services.AddTransient<IPrincipal>(cfg =>
            {

                var httpContext = cfg.GetService<IHttpContextAccessor>();

                var user = httpContext?.HttpContext?.User;

                var isAuthenticated = user?.Identity?.IsAuthenticated ?? false;

                if (isAuthenticated)
                {
                    return httpContext.HttpContext.User;
                }

                if (!(user is null))
                {
                    var identity = new GenericIdentity("Anonymous", "Anonymous");
                    return new GenericPrincipal(identity, new string[] { });
                }
                else
                {
                    var identity = new GenericIdentity("Unknown", "Anonymous");
                    return new GenericPrincipal(identity, new string[] { });
                }
                
               
            });

           
           
        }
    }
}
