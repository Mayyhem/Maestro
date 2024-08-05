using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Linq;

namespace Maestro
{
    public class HttpHandler
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

        public async Task<List<T>> GetMSGraphEntities<T>(
            string baseUrl,
            Func<JObject, T> entityCreator,
            bool count = false,
            List<(string LogicalOperator, string Key, string ComparisonOperator, string Value)> filters = null,
            string search = null,
            string[] properties = null,
            LiteDBHandler database = null,
            bool printJson = true) where T : class
        {
            var entities = new List<T>();
            var urlBuilder = new MSGraphUrlBuilder(baseUrl);

            // Add count
            if (count)
            {
                urlBuilder.AddCount();
            }

            // Add filters
            if (filters != null && filters.Any())
            {
                string filterString = urlBuilder.BuildFilterString(filters);
                urlBuilder.AddFilter(filterString);
            }

            // Add search
            if (search != null)
            {
                urlBuilder.AddSearch(search);
            }

            // Add properties
            if (properties != null)
            {
                urlBuilder.AddSelect(properties);
            }

            string url = urlBuilder.Build();

            // Request entities from Graph API
            Logger.Info($"Requesting {typeof(T).Name}s from Microsoft Graph");
            HttpResponseMessage response = await GetAsync(url);
            if (response == null)
            {
                Logger.Error($"Failed to get {typeof(T).Name}s from Microsoft Graph");
                return null;
            }

            // Deserialize the JSON response
            string responseContent = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                Logger.Error($"Bad request: {response.Content}");
                return null;
            }
            JObject responseObject = JObject.Parse(responseContent);

            var entitiesArray = responseObject["value"] as JArray ?? new JArray { responseObject };

            if (entitiesArray.Count == 0)
            {
                Logger.Info($"No matching {typeof(T).Name}s found");
                return null;
            }

            Logger.Info($"Found {entitiesArray.Count} matching {(entitiesArray.Count == 1 ? typeof(T).Name : typeof(T).Name + "s")} in Microsoft Graph");

            foreach (JObject entityJson in entitiesArray)
            {
                var entity = entityCreator(entityJson);
                entities.Add(entity);

                // Upsert each entity to the database
                if (database != null)
                {
                    bool upsertResult = database.Upsert(entity);
                    if (!upsertResult)
                    {
                        Logger.Warning($"Failed to upsert {typeof(T).Name} to database");
                    }
                    else
                    {
                        Logger.Verbose($"Upserted {(entitiesArray.Count == 1 ? typeof(T).Name : typeof(T).Name + "s")} in the database");
                    }
                }
            }

            // Print the selected properties of the entities
            if (printJson)
            {
                string entitiesJson = JsonConvert.SerializeObject(entitiesArray, Formatting.Indented);
                JsonHandler.PrintProperties(entitiesJson, properties);
            }

            return entities;
        }

        public async Task<HttpResponseMessage> PatchAsync(string url, HttpContent content = null)
        {
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), url) { Content = content };
            return await SendRequestAsync(request);
        }

        public async Task<HttpResponseMessage> PostAsync(string url, HttpContent content = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            return await SendRequestAsync(request);
        }

        public async Task<HttpResponseMessage> PutAsync(string url, HttpContent content = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, url) { Content = content };
            return await SendRequestAsync(request);
        }

        public void RemoveHeader(string headerName)
        {
            _httpClient.DefaultRequestHeaders.Remove(headerName);
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
                if (response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Unauthorized)
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

        public void SetHeader(string headerName, string headerValue)
        {
            _httpClient.DefaultRequestHeaders.Add(headerName, headerValue);
        }
    }
}
