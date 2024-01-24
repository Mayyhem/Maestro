using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

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
        public async Task<string> GetAsync(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            return await SendRequestAsync(request);
        }

        public async Task<string> PostAsync(string url, HttpContent content)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            return await SendRequestAsync(request);
        }

        private async Task<string> SendRequestAsync(HttpRequestMessage request)
        {
            string responseContent = string.Empty;

            Logger.Info($"Sending {request.Method} request to: {request.RequestUri}");
            try
            {
                HttpResponseMessage response = await _httpClient.SendAsync(request);

                // Throw HttpRequestException if the response is not successful
                response.EnsureSuccessStatusCode();  
                responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Logger.Error(response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                        ? "Access denied: Unauthorized request"
                        : $"Error: {response.StatusCode}\n Response: {responseContent}");
                    return null;
                }
                Logger.Info($"Received {response.StatusCode} status code from: {request.RequestUri}");
                Logger.Debug($"Response:\n {responseContent}");
            }
            catch (HttpRequestException ex)
            {
                Logger.ExceptionDetails(ex);
            }
            return responseContent;
        }

        public void SetAuthorizationHeader(string bearerToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }
    }
}