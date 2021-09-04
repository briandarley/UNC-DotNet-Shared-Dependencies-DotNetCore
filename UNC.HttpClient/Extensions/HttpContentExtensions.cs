using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
//using JsonSerializer = System.Text.Json.JsonSerializer;

namespace UNC.HttpClient.Extensions
{
    public static class HttpContentExtensions
    {
        public static async Task<T> ReadAsAsync<T>(this HttpContent content)
        {
            await using var stream = await content.ReadAsStreamAsync();
            var jsonReader = new JsonTextReader(new StreamReader(stream));
            var jsonSerializer = new JsonSerializer();
            return jsonSerializer.Deserialize<T>(jsonReader);
        }



    }
}
