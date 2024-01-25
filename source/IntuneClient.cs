using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Maestro
{
    public class IntuneClient
    {
        private readonly IHttpHandler _httpHandler;

        public IntuneClient(IHttpHandler httpHandler)
        {
            _httpHandler = httpHandler;
        }

        public async Task<string> ListEnrolledDevicesAsync()
        {
            // Define the specific URI for Intune-enrolled devices
            string intuneDevicesUrl = "https://graph.microsoft.com/beta/me/managedDevices";
            return await _httpHandler.GetAsync(intuneDevicesUrl);
        }

        public async Task<string> CreateScriptPackage(string displayName, string detectionScriptContent, string description = "", string publisher = "", string remediationScriptContent = "", bool runAs32Bit = true, string runAsAccount = "system")
        {
            string url = "https://graph.microsoft.com/beta/deviceManagement/deviceHealthScripts";
            var jsonObject = new
            {
                displayName,
                description,
                publisher,
                runAs32Bit,
                runAsAccount,
                enforceSignatureCheck = "false",
                detectionScriptContent,
                remediationScriptContent,
                roleScopeTagIds = new List<string> { "0" }
            };
            var serializer = new JavaScriptSerializer();
            string json = serializer.Serialize(jsonObject);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            string response = await _httpHandler.PostAsync(url, content);
            if (response is null)
            {
                return null;
            }
            string scriptId = Util.GetMatch(response, "\"id\":\"([^\"]+)\"");
            return scriptId;
        }
    }
}
