using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Maestro
{
    public interface IHttpHandler
    { 
        CookieContainer CookiesContainer { get; set; }
        StringContent CreateJsonContent(object jsonObject);
        Task<string> DeleteAsync(string url);
        Task<string> GetAsync(string url);
        Task<string> PostAsync(string url, HttpContent content = null);
        Task<string> SendRequestAsync(HttpRequestMessage message);
        void SetAuthorizationHeader(string bearerToken);
    }
}
