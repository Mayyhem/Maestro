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
using LiteDB;
using System.ComponentModel;

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

        public async Task<HttpResponseMessage> GetAsync(string url, bool jsonResponse = false)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            return await SendRequestAsync(request, jsonResponse);
        }

        public async Task<List<T>> GetMSGraphEntities<T>(
            string baseUrl,
            Func<JObject, T> entityCreator,
            bool count = false,
            List<(string LogicalOperator, string Key, string ComparisonOperator, string Value)> filters = null,
            string expand = null,
            string search = null,
            string[] properties = null,
            LiteDBHandler database = null,
            bool printJson = true) where T : class
        {
            // Get all fields of type T -- can't use class properties because they need to be automatically generated
            var typeFields = typeof(T).GetFields().Select(f => f.Name).ToList();

            // Don't add "Properties" inherited from JsonObject
            typeFields.Remove("Properties");
            
            // Combine user-specified properties with type properties, ensuring uniqueness
            var propertiesFilter = new HashSet<string>(typeFields, StringComparer.OrdinalIgnoreCase);


            if (properties != null)
            {
                foreach (var prop in properties)
                {
                     propertiesFilter.Add(prop);
                }
            }

            var entities = new List<T>();
            var urlBuilder = new MSGraphUrl(baseUrl);

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

            // Add expand
            if (expand != null)
            {
                urlBuilder.AddExpand(expand);
                foreach (var expandProperty in expand.Split(','))
                {
                    propertiesFilter.Add(expandProperty);
                }
            }

            // Add search
            if (search != null)
            {
                urlBuilder.AddSearch(search);
            }

            // Add properties
            bool returnAllProperties = properties != null && properties.Any(p => p.Equals("ALL", StringComparison.OrdinalIgnoreCase));

            if (properties != null && properties.Any() && !returnAllProperties)
            {
                urlBuilder.AddSelect(propertiesFilter.ToArray());
            }

            string url = urlBuilder.Build();

            // Request entities from Graph API
            Logger.Info($"Requesting {typeof(T).Name}s from Microsoft Graph");
            HttpResponseMessage response = await GetAsync(url, true);
            if (response == null)
            {
                Logger.Error($"Failed to get {typeof(T).Name}s from Microsoft Graph");
                return null;
            }

            // Deserialize the JSON response
            string responseContent = await response.Content.ReadAsStringAsync();

            // Parse the response
            JObject responseObject = JObject.Parse(responseContent);
            var entitiesArray = responseObject["value"] as JArray ?? new JArray { responseObject };
            if (entitiesArray.Count == 0)
            {
                Logger.Warning($"No matching {typeof(T).Name}s found");
                return null;
            }

            Logger.Info($"Found {entitiesArray.Count} {(entitiesArray.Count == 1 ? typeof(T).Name : typeof(T).Name + "s")} matching query in Microsoft Graph");

            // Add each item to the database
            foreach (JObject entityJson in entitiesArray)
            {
                var entity = entityCreator(entityJson);
                entities.Add(entity);
            }

            // Print the selected properties of the entities
            if (printJson)
            {
                string entitiesJson = JsonConvert.SerializeObject(entitiesArray, Formatting.Indented);
                JsonHandler.GetProperties(entitiesJson, false, propertiesFilter.ToArray());
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
            if (_httpClient.DefaultRequestHeaders.Contains(headerName))
            {
                _httpClient.DefaultRequestHeaders.Remove(headerName);
            }
        }

        public async Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request, bool jsonResponse = false)
        {
            Logger.Verbose($"Sending {request.Method} request to: {request.RequestUri}");
            HttpResponseMessage response = await _httpClient.SendAsync(request);

            if (response != null)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound
                    || response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Logger.Error($"Received {response.StatusCode.GetHashCode()} ({response.StatusCode}) status code from: {request.RequestUri}");
                    if (jsonResponse)
                    {
                        JsonHandler.GetProperties(responseContent);
                    }
                    else
                    {
                        Logger.Error(responseContent);
                    }
                    return null;
                }
                Logger.Verbose($"Received {response.StatusCode.GetHashCode()} ({response.StatusCode}) status code from: {request.RequestUri}");
                Logger.Debug($"Response:\n{responseContent}");
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
            if (!_httpClient.DefaultRequestHeaders.Contains(headerName))
            {
                _httpClient.DefaultRequestHeaders.Add(headerName, headerValue);

            }
        }
    }
}
