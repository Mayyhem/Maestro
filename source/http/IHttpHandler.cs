using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Maestro
{
    public interface IHttpHandler
    { 
        CookieContainer CookiesContainer { get; set; }
        StringContent CreateJsonContent(object jsonObject);
        Task<HttpResponseMessage> DeleteAsync(string url);
        Task<HttpResponseMessage> GetAsync(string url);
        Task<HttpResponseMessage> PatchAsync(string url, HttpContent content = null);
        Task<HttpResponseMessage> PostAsync(string url, HttpContent content = null);
        Task<HttpResponseMessage> PutAsync(string url, HttpContent content = null);
        Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage message);
        void SetAuthorizationHeader(string bearerToken);
    }
}
