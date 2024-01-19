using System;
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
            try
            {
                Logger.Info($"Sending {request.Method} request to: {request.RequestUri}");
                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        Logger.Error("Access denied. Unauthorized request.");
                        throw new UnauthorizedAccessException("Access denied. Unauthorized request.");
                    }
                    else
                    {
                        throw new HttpRequestException($"Error: {response.StatusCode}. Details: {errorContent}");
                    }
                }

                Logger.Info($"Received {response.StatusCode} status code from: {request.RequestUri}");
                Logger.Debug(await response.Content.ReadAsStringAsync());
                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException e)
            {
                throw new Exception($"HTTP Request Exception: {e.Message}", e);
            }
            catch (Exception e)
            {
                throw new Exception($"Exception: {e.Message}", e);
            }
        }

        public void SetAuthorizationHeader(string bearerToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }
    }
}