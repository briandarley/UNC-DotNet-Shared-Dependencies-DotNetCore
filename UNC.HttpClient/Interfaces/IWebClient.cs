using System;
using System.Threading.Tasks;
using UNC.HttpClient.EventHandlers;
using UNC.HttpClient.Models;

namespace UNC.HttpClient.Interfaces
{
    public interface IWebClient
    {
        public static TokenResponse AuthResponse { get; set; }
        EventHandler<TokenResponse> TokenRefreshed { get; set; }
        EventHandler<WebRequestException> WebRequestErrorHandler { get; set; }
        bool PreventLogging { get; set; }
        string BaseAddress { get; set; }
        int Timeout { get; set; }
        bool DefaultEnsureSuccessStatusCode { get; set; }

        Task<bool> DeleteEntity(string path);
        Task<bool> EnsureSuccessStatusCode(string path);
        Task<T> GetEntity<T>(string path);
        Task<string> GetRaw(string path = "");

        Task<bool> Post(string path, object entity = null, bool putByQueryParameter = false);
        Task<T> Post<T>(string path, object entity = null, bool postByQueryParameter = false);
        Task<T> Put<T>(string path, object entity = null, bool putByQueryParameter = false);
        Task<bool> Put(string path, object entity = null, bool putByQueryParameter = false);
        Task<T> Patch<T>(string path, object entity = null, bool putByQueryParameter = false);
        Task<bool> Patch(string path, object entity = null, bool putByQueryParameter = false);

        string ToString();
    }
}
