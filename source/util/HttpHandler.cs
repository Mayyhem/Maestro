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
        public int LastStatusCode { get; private set; }

        public HttpHandler()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

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

        public async Task<HttpResponseMessage> DeleteAsync(string url, bool isJsonResponse = true)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, url);
            return await SendRequestAsync(request, isJsonResponse);
        }

        public async Task<HttpResponseMessage> GetAsync(string url, bool isJsonResponse = true)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            return await SendRequestAsync(request, isJsonResponse);
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
            bool printJson = true,
            bool raw = false) where T : class
        {
            // Only print if the entities contain properties specified by the user
            List<string> userSpecifiedProperties = properties?.ToList() ?? new List<string>();

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

            bool returnAllProperties = properties != null && properties.Any(p => p.Equals("ALL", StringComparison.OrdinalIgnoreCase));

            if (properties != null && properties.Any() && !returnAllProperties)
            {
                urlBuilder.AddSelect(propertiesFilter.ToArray());
            }

            string url = urlBuilder.Build();

            // Request entities from Graph API
            Logger.Info($"Requesting {typeof(T).Name}s from Microsoft Graph");
            HttpResponseMessage response = await GetAsync(url, isJsonResponse: true);
            if (response == null)
            {
                Logger.Error($"Failed to get {typeof(T).Name}s from Microsoft Graph");
                return null;
            }

            // Deserialize the JSON response
            string responseContent = await response.Content.ReadAsStringAsync();

            // Parse the response
            JObject responseObject = JObject.Parse(responseContent);

            // Check for errors in the response
            if (responseObject.ContainsKey("error"))
            {
                string message = responseObject["error"]["message"].ToString();
                string prettyMessage = JsonHandler.GetProperties(message, print: false);
                Logger.Error($"Error in Microsoft Graph response:\n{prettyMessage}");
                return null;
            }

            var entitiesArray = responseObject["value"] as JArray ?? new JArray { responseObject };
            if (entitiesArray.Count == 0)
            {
                Logger.Warning($"No matching {typeof(T).Name}s found");
                return null;
            }

            Logger.Info($"Found {entitiesArray.Count} {(entitiesArray.Count == 1 ? typeof(T).Name : typeof(T).Name + "s")} matching query in Microsoft Graph");

            List<string> propsRequiringExpansion = new List<string>();

            // Add each item to the database
            foreach (JObject entityJson in entitiesArray)
            {
                // Check if any user-specified properties are not present in the entity
                if (userSpecifiedProperties.Any())
                {
                    var entityProperties = entityJson.Properties().Select(p => p.Name).ToList();

                    if (!userSpecifiedProperties.All(entityProperties.Contains))
                    {
                        propsRequiringExpansion = userSpecifiedProperties.Except(entityProperties).ToList();
                    }
                }
                var entity = entityCreator(entityJson);
                entities.Add(entity);
            }

            // Print the selected properties of the entities
            if (printJson)
            {
                JsonHandler.GetProperties(entitiesArray, raw, propertiesFilter.ToArray());
            }

            // Remove "all" (case-insensitive) from the properties list
            propsRequiringExpansion = propsRequiringExpansion
                .Where(prop => !prop.Equals("all", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (propsRequiringExpansion.Any())
            {
                Logger.Warning($"The following properties were present but may require expansion: {string.Join(", ", propsRequiringExpansion)}");
            }

            return entities;
        }

        public async Task<HttpResponseMessage> PatchAsync(string url, HttpContent content = null, bool isJsonResponse = true)
        {
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), url) { Content = content };
            return await SendRequestAsync(request, isJsonResponse);
        }

        public async Task<HttpResponseMessage> PostAsync(string url, HttpContent content = null, bool isJsonResponse = true)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            return await SendRequestAsync(request, isJsonResponse);
        }

        public async Task<HttpResponseMessage> PutAsync(string url, HttpContent content = null, bool isJsonResponse = true)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, url) { Content = content };
            return await SendRequestAsync(request, isJsonResponse);
        }

        public void RemoveHeader(string headerName)
        {
            if (_httpClient.DefaultRequestHeaders.Contains(headerName))
            {
                _httpClient.DefaultRequestHeaders.Remove(headerName);
            }
        }

        public async Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request, bool isJsonResponse = true)
        {
            Logger.Verbose($"Sending {request.Method} request to: {request.RequestUri}");
            HttpResponseMessage response = await _httpClient.SendAsync(request);

            if (response != null)
            {
                LastStatusCode = (int)response.StatusCode;
                string responseContent = await response.Content.ReadAsStringAsync();
                if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound
                    || response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Logger.Warning($"Received {response.StatusCode.GetHashCode()} ({response.StatusCode}) status code from: {request.RequestUri}");
                    if (isJsonResponse)
                    {
                        string formattedJson = JsonHandler.GetProperties(responseContent, print: false);
                        Logger.DebugTextOnly(formattedJson);
                    }
                }
                else
                {
                    Logger.Verbose($"Received {response.StatusCode.GetHashCode()} ({response.StatusCode}) status code from: {request.RequestUri}");
                }
                Logger.DebugTextOnly(responseContent);
            }
            else
            {
                Logger.Warning($"No response received from {request.RequestUri}");
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
