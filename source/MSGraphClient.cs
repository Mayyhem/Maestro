using System.Threading.Tasks;

namespace Maestro
{
    public class MSGraphClient
    {
        private readonly IHttpHandler _httpHandler;

        public MSGraphClient(IHttpHandler httpHandler)
        {
            _httpHandler = httpHandler;
        }

        public async Task<string> GetAsync(string url)
        {
            string response = await _httpHandler.GetAsync(url);
            return response;
        }
    }
}