using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;

namespace UNC.API.Base.Security
{
    /// <summary>
    /// Used for authorization, set this up in the startup of your application
    /// </summary>
    public class AthorizationControllerConvention : IControllerModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            controller.Filters.Add(new AuthorizeFilter());
        }
    }
}
