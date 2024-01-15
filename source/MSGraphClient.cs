using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Maestro
{
    public class MSGraphClient
    {
        private readonly HttpClient _httpClient;

        public MSGraphClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GetAsync(string uri)
        {
            try
            {
                var response = await _httpClient.GetAsync(uri);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        throw new UnauthorizedAccessException("Access denied. Unauthorized request.");
                    }
                    else
                    {
                        throw new HttpRequestException($"Error: {response.StatusCode}. Details: {errorContent}");
                    }
                }
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
    }
}