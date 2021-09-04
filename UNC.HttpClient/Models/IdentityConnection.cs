using System;
using UNC.HttpClient.Interfaces;

namespace UNC.HttpClient.Models
{
    public class IdentityConnection:IAuthSettings
    {
        public string IdentityServer { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Scope { get; set; }
    }
}
