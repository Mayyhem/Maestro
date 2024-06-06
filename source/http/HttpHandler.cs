using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Maestro
{
    public class HttpHandler : IHttpHandler
    {
        private readonly HttpClient _httpClient;
        public CookieContainer CookiesContainer { get; set; }

        public HttpHandler()
        {
            var httpClientHandler = new HttpClientHandler
            {
                CookieContainer = new CookieContainer()
            };

            _httpClient = new HttpClient(httpClientHandler);
            CookiesContainer = httpClientHandler.CookieContainer;
        }

        public StringContent CreateJsonContent(object jsonObject)
        {
            var serializer = new JavaScriptSerializer();
            string json = serializer.Serialize(jsonObject);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        public async Task<HttpResponseMessage> DeleteAsync(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, url);
            return await SendRequestAsync(request);
        }

        public async Task<HttpResponseMessage> GetAsync(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            return await SendRequestAsync(request);
        }

        public async Task<HttpResponseMessage> PostAsync(string url, HttpContent content = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            return await SendRequestAsync(request);
        }

        public async Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request)
        {
            Logger.Verbose($"Sending {request.Method} request to: {request.RequestUri}");
            HttpResponseMessage response = await _httpClient.SendAsync(request);

            if (response != null)
            {
                Logger.Verbose($"Received {response.StatusCode.GetHashCode()} ({response.StatusCode}) status code from: {request.RequestUri}");
                string responseContent = await response.Content.ReadAsStringAsync();
                Logger.Debug($"Response:\n {responseContent}");
                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    Logger.Error("Unauthorized: try reauthenticating (--reauth) or providing different credentials");
                    JsonHandler.PrintProperties(responseContent);
                    return null;
                }
            }
            else
            {
                Logger.Error($"No response received from {request.RequestUri}");
            }
            return response;
        }

        public void SetAuthorizationHeader(string bearerToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }
    }
}
