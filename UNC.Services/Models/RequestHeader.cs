using System.Security.Principal;

namespace UNC.Services.Models
{
    public class RequestHeader
    {
    
        public string ApplicationName { get; set; }

        public string AuthUser => Principal?.Identity?.Name;

      
    
        public IPrincipal Principal { get; set; }
    }
}
