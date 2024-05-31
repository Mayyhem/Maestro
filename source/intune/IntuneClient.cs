using LiteDB;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Maestro
{
    // Intune Microsoft Graph client
    public class IntuneClient
    {

        private readonly IAuthClient _authClient;
        private readonly IHttpHandler _httpHandler;
        public string BearerToken;

        public IntuneClient()
        {
            _httpHandler = new HttpHandler();
            _authClient = new AuthClient(_httpHandler);
        }

        public IntuneClient(IAuthClient authClient)
        {
            _authClient = authClient;
            _httpHandler = authClient.HttpHandler;
        }

        public async Task DeleteDeviceAssignmentFilter(string filterId)
        {

            Logger.Info($"Deleting device assignment filter with ID: {filterId}");
            string url = $"https://graph.microsoft.com/beta/deviceManagement/assignmentFilters/{filterId}";
            await _httpHandler.DeleteAsync(url);
        }

        public async Task DeleteScriptPackage(string scriptId)
        {
            Logger.Info($"Deleting detection script with scriptId: {scriptId}");
            string url = $"https://graph.microsoft.com/beta/deviceManagement/deviceHealthScripts/{scriptId}";
            await _httpHandler.DeleteAsync(url);
        }

        public async Task DeviceQuery(string query, string deviceId = "", string deviceName = "", IDatabaseHandler database = null)
        {
            Logger.Info($"Executing device query: {query}");
            if (!string.IsNullOrEmpty(deviceId))
            {
                string url = $"https://graph.microsoft.com/beta/deviceManagement/managedDevices/{deviceId}/createQuery";
                string encodedQuery = Convert.ToBase64String(Encoding.UTF8.GetBytes(query));
                var content = _httpHandler.CreateJsonContent(new
                {
                    query = encodedQuery
                });
                await _httpHandler.PostAsync(url, content);
            }
            else if (!string.IsNullOrEmpty(deviceName))
            {
                throw new NotImplementedException();
            }
        }

        public async Task<string> GetAccessToken(string tenantId, string portalAuthorization)
        {
            Logger.Info("Requesting Intune access token");
            string intuneAccessToken = await _authClient.GetAccessToken(_authClient.TenantId, _authClient.RefreshToken,
                "https://intune.microsoft.com/api/DelegationToken",
                "Microsoft_Intune_DeviceSettings", "microsoft.graph");
            if (intuneAccessToken is null) return null;

            _httpHandler.SetAuthorizationHeader(intuneAccessToken);

            BearerToken = intuneAccessToken;
            return intuneAccessToken;
        }

        public async Task GetDevices(string deviceId = "", string deviceName = "", string[] properties = null, IDatabaseHandler database = null, bool databaseOnly = false)
        {
            string intuneDevicesUrl = "https://graph.microsoft.com/beta/me/managedDevices";

            if (!string.IsNullOrEmpty(deviceId))
            {
                if (database != null)
                {
                    var device = database.FindByPrimaryKey<IntuneDevice>(deviceId);
                    if (device != null)
                    {
                        Logger.Info($"Found a matching device in the database");
                        JsonHandler.PrintProperties(device.ToString(), properties);
                        return;
                    }
                }
                intuneDevicesUrl = $"https://graph.microsoft.com/beta/me/managedDevices?filter=deviceId%20eq%20%27{deviceId}%27";
            }
            else if (!string.IsNullOrEmpty(deviceName))
            {
                if (database != null)
                {
                    var databaseDevices = database.FindInCollection<IntuneDevice>("deviceName", deviceName);
                    if (databaseDevices.Any())
                    {
                        Logger.Info($"Found {databaseDevices.Count()} matching devices in the database");
                        foreach (var device in databaseDevices)
                        {
                            JsonHandler.PrintProperties(device.ToString(), properties);
                        }
                        return;
                    }
                    else
                    {
                        Logger.Info("No matching devices found in the database");
                    }
                }
                intuneDevicesUrl = $"https://graph.microsoft.com/beta/me/managedDevices?filter=deviceName%20eq%20%27{deviceName}%27";
            }
            else
            {
                if (database != null)
                {
                    var databaseDevices = database.FindInCollection<IntuneDevice>();
                    if (databaseDevices.Any())
                    {
                        Logger.Info($"Found {databaseDevices.Count()} matching devices in the database");
                        foreach (var device in databaseDevices)
                        {
                            JsonHandler.PrintProperties(device.ToString(), properties);
                        }
                        return;
                    }
                    else
                    {
                        Logger.Info("No matching devices found in the database");
                    }
                    if (databaseOnly)
                    {
                        return;
                    }
                }
            }

            // Request devices from Intune
            await SignInToIntuneAndGetAccessToken(database);
            string devicesResponse = await _httpHandler.GetAsync(intuneDevicesUrl);

            // Deserialize the JSON response to a dictionary
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            var deviceResponseDict = serializer.Deserialize<Dictionary<string, object>>(devicesResponse);
            var devices = (ArrayList)deviceResponseDict["value"];

            if (devices.Count > 0)
            {
                Logger.Info($"Found {devices.Count} devices in Intune");
                foreach (Dictionary<string, object> device in devices)
                {
                    // Create an IntuneDevice object for each device in the response
                    var intuneDevice = new IntuneDevice(device);
                    if (database != null)
                    {
                        // Insert new device if matching id doesn't exist
                        database.Upsert(intuneDevice);
                    }
                }
                // Convert the devices ArrayList to JSON blob string
                string devicesJson = JsonConvert.SerializeObject(devices, Formatting.Indented);

                // Print the selected properties of the devices
                JsonHandler.PrintProperties(devicesJson, properties);
            }
            else
            {
                Logger.Info("No devices found");
            }
        }

        public async Task InitiateOnDemandProactiveRemediation(string deviceId, string scriptId)
        {
            Logger.Info($"Initiating on demand proactive remediation - execution script {scriptId} on device {deviceId}");
            string url =
                $"https://graph.microsoft.com/beta/deviceManagement/managedDevices('{deviceId}')/initiateOnDemandProactiveRemediation";
            var content = _httpHandler.CreateJsonContent(new
            {
                ScriptPolicyId = scriptId,
            });
            await _httpHandler.PostAsync(url);
        }

        public async Task<string> NewDeviceAssignmentFilter(string deviceName)
        {
            string displayName = Guid.NewGuid().ToString();
            Logger.Info($"Creating new device assignment filter with displayName: {displayName}");

            string url = "https://graph.microsoft.com/beta/deviceManagement/assignmentFilters";
            var content = _httpHandler.CreateJsonContent(new
            {
                displayName,
                description = "",
                platform = "Windows10AndLater",
                rule = $"(device.deviceName -eq \"{deviceName}\")",
                roleScopeTagIds = new List<string> { "0" }
            });
            string response = await _httpHandler.PostAsync(url, content);
            if (response is null) return null;

            string filterId = StringHandler.GetMatch(response, "\"id\":\"([^\"]+)\"");
            Logger.Info($"Obtained filter ID: {filterId}");
            return filterId;
        }

        public async Task NewDeviceManagementScriptAssignment(string filterId, string scriptId)
        {
            // Start script 5 minutes from now to account for sync
            var now = DateTime.UtcNow.AddMinutes(5);
            Logger.Info($"Assigning script {scriptId} to filter {filterId}");
            string url = $"https://graph.microsoft.com/beta/deviceManagement/deviceHealthScripts/{scriptId}/assign";

            var content = _httpHandler.CreateJsonContent(new
            {
                deviceHealthScriptAssignments = new[]
                {
                    new
                    {
                        target = new Dictionary<string, object>
                        {
                            { "@odata.type", "#microsoft.graph.allDevicesAssignmentTarget" },
                            { "deviceAndAppManagementAssignmentFilterId", filterId },
                            { "deviceAndAppManagementAssignmentFilterType", "include" }
                        },
                        runRemediationScript = true,
                        runSchedule = new Dictionary<string, object>
                        {
                            { "@odata.type", "#microsoft.graph.deviceHealthScriptRunOnceSchedule" },
                            { "interval", 1 },
                            { "date", now.ToString("yyyy-MM-dd") },
                            { "time", now.ToString("HH:mm:ss") },
                            { "useUtc", true }
                        }
                    }
                }
            });
            await _httpHandler.PostAsync(url, content);
        }

        public async Task NewDeviceManagementScriptAssignmentHourly(string filterId, string scriptId)
        {
            // Start script 5 minutes from now to account for sync
            var now = DateTime.UtcNow.AddMinutes(5);
            Logger.Info($"Assigning script {scriptId} to filter {filterId}");
            string url = $"https://graph.microsoft.com/beta/deviceManagement/deviceHealthScripts/{scriptId}/assign";

            var content = _httpHandler.CreateJsonContent(new
            {
                deviceHealthScriptAssignments = new[]
                {
                    new
                    {
                        target = new Dictionary<string, object>
                        {
                            { "@odata.type", "#microsoft.graph.allDevicesAssignmentTarget" },
                            { "deviceAndAppManagementAssignmentFilterId", filterId },
                            { "deviceAndAppManagementAssignmentFilterType", "include" }
                        },
                        runRemediationScript = true,
                        runSchedule = new Dictionary<string, object>
                        {
                            { "@odata.type", "#microsoft.graph.deviceHealthScriptHourlySchedule" },
                            { "interval", 1 }
                        }
                    }
                }
            });
            await _httpHandler.PostAsync(url, content);
        }

        public async Task<string> NewScriptPackage(string displayName, string detectionScriptContent, string description = "", string publisher = "", string remediationScriptContent = "", bool runAs32Bit = true, string runAsAccount = "system")
        {
            Logger.Info($"Creating new detection script with displayName: {displayName}");
            string url = "https://graph.microsoft.com/beta/deviceManagement/deviceHealthScripts";

            var content = _httpHandler.CreateJsonContent(new
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
            });
            string response = await _httpHandler.PostAsync(url, content);
            if (response is null) return null;

            string scriptId = StringHandler.GetMatch(response, "\"id\":\"([^\"]+)\"");
            Logger.Info($"Obtained script ID: {scriptId}");
            return scriptId;
        }

        public bool FindStoredAccessToken(IDatabaseHandler database)
        {
            if (!string.IsNullOrEmpty(BearerToken))
            {
                Logger.Info("Using bearer token from prior request");
                return true;
            }
            if (database != null)
            {
                var validJwtDoc = database.FindValidJwt<BsonDocument>();

                if (validJwtDoc != null)
                {
                    Logger.Info($"Found JWT with the required scope in the database");
                    BearerToken = validJwtDoc["bearerToken"];
                    _httpHandler.SetAuthorizationHeader(BearerToken);
                    return true;
                }
                else
                {
                    Logger.Info("No JWTs with the required scope found in the database");
                }
            }
            return false;
        }

        public async Task SignInToIntuneAndGetAccessToken(IDatabaseHandler database = null)
        {
            // Check for stored access token before fetching from Intune
            bool foundJwt = FindStoredAccessToken(database);
            if (foundJwt)
            {
                return;
            }

            // Request access token from Intune if none found
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            await _authClient.GetTenantIdAndRefreshToken();
            string base64Token = await GetAccessToken(_authClient.TenantId, _authClient.RefreshToken);

            // Store new JWT in the IntuneClient object
            BearerToken = base64Token;

            // Store new JWT in the database
            if (database != null)
            {
                Jwt accessToken = new Jwt(base64Token);
                database.Upsert(accessToken);
            }
        }

        public async Task SyncDevice(string deviceId, IDatabaseHandler database)
        {
            await SignInToIntuneAndGetAccessToken(database);
            Logger.Info($"Intune will attempt to check in and sync actions and policies to device with device {deviceId}");
            string url = $"https://graph.microsoft.com/beta/deviceManagement/managedDevices('{deviceId}')/syncDevice";
            string response = await _httpHandler.PostAsync(url);
        }
    }
}
