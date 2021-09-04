

namespace UNC.HttpClient.Interfaces
{
    public interface IAuthSettings
    {
        public string IdentityServer { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Scope { get; set; }
    }
}
