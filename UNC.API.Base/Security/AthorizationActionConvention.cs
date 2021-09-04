using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;

namespace UNC.API.Base.Security
{
    public class AthorizationActionConvention : IActionModelConvention
    {
        /// <summary>
        /// Used for authorization, set this up in the startup of your application
        /// Applies authorization filter requirements policy="write" to PUT/POST/DELETE methods
        /// Applies authorization filter requirements policy="read" to GET methods
        ///
        /// Sample implementation, within Startup/ConfigureServices
        /// services.AddControllers(
        /// opt =>
        /// {
        ///     //Custom filters can be added here 
        ///     //opt.Filters.Add(typeof(CustomFilterAttribute));
        ///     //opt.Filters.Add(new ProducesAttribute("application/json"));
        ///     opt.Conventions.Add(new AthorizationControllerConvention());
        ///     opt.Conventions.Add(new AthorizationActionConvention());
        /// })
        /// </summary>
        public void Apply(ActionModel action)
        {
            //Require specific claims for mutable actions    
            if (action.Attributes.Any(a => a is HttpPostAttribute || a is HttpPutAttribute || a is HttpDeleteAttribute))
            {
                action.Filters.Add(new AuthorizeFilter(policy: "write"));
            }
            else if (action.Attributes.Any(a => a is HttpGetAttribute))
            {
                action.Filters.Add(new AuthorizeFilter(policy: "read"));
                
            }
        }
    }
}
