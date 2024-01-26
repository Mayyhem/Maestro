using System;
using System.Collections.Generic;
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

        public async Task<string> GetAsync(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            return await SendRequestAsync(request);
        }

        public async Task<string> PostAsync(string url, HttpContent content = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            return await SendRequestAsync(request);
        }

        public async Task<string> SendRequestAsync(HttpRequestMessage request)
        {
            string responseContent = string.Empty;
            Logger.Info($"Sending {request.Method} request to: {request.RequestUri}");

            HttpResponseMessage response = await _httpClient.SendAsync(request);

            if (response != null)
            {
                Logger.Info($"Received {response.StatusCode.GetHashCode()} ({response.StatusCode}) status code from: {request.RequestUri}");
                responseContent = await response.Content.ReadAsStringAsync();
                Logger.Debug($"Response:\n {responseContent}");

                // Throw HttpRequestException if the response is not successful
                response.EnsureSuccessStatusCode();
                if (response.IsSuccessStatusCode) return responseContent;
            }
            Logger.Error(response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                ? "Access denied: Unauthorized request"
                : $"Error: {response.StatusCode}");
            return null;
        }

        public void SetAuthorizationHeader(string bearerToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }
    }
}