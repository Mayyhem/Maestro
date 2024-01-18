using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Maestro
{
    public interface IHttpHandler
    { 
        CookieContainer CookiesContainer { get; set; }
        Task<string> GetAsync(string url);
        Task<string> PostAsync(string actionUrl, FormUrlEncodedContent formData);
    }
}
