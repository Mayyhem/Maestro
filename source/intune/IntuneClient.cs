using LiteDB;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Maestro
{
    public class IntuneClient
    {

        private GraphClient _graphClient;
        public IntuneClient() 
        { 
            _graphClient = new GraphClient();
        }

        public static async Task<IntuneClient> InitAndGetAccessToken(IDatabaseHandler database, string bearerToken = "", bool reauth = false)
        {
            var intuneClient = new IntuneClient();
            string authRedirectUrl = "https://intune.microsoft.com/signin/idpRedirect.js";
            string delegationTokenUrl = "https://intune.microsoft.com/api/DelegationToken";
            string extensionName = "Microsoft_Intune_DeviceSettings";
            intuneClient._graphClient = await GraphClient.InitAndGetAccessToken<GraphClient>(authRedirectUrl, delegationTokenUrl, extensionName, 
                database, bearerToken, reauth);
            return intuneClient;
        }

        public async Task DeleteDeviceAssignmentFilter(string filterId)
        {

            Logger.Info($"Deleting device assignment filter with ID: {filterId}");
            string url = $"https://graph.microsoft.com/beta/deviceManagement/assignmentFilters/{filterId}";
            await _graphClient._httpHandler.DeleteAsync(url);
        }

        public async Task DeleteScriptPackage(string scriptId)
        {
            Logger.Info($"Deleting detection script with scriptId: {scriptId}");
            string url = $"https://graph.microsoft.com/beta/deviceManagement/deviceHealthScripts/{scriptId}";
            HttpResponseMessage response = await _graphClient._httpHandler.DeleteAsync(url);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                Logger.Error("Failed to delete detection script");
                return;
            }
            Logger.Info("Successfully deleted detection script");
        }

        public async Task ExecuteDeviceQuery(string kustoQuery, int maxRetries, int retryDelay, string deviceId = "", string deviceName = "", IDatabaseHandler database = null)
        {
            // Check whether the device exists in Intune
            IntuneDevice device = await GetDevice(deviceId, deviceName, database: database);

            // Submit the query to Intune
            string queryId = await NewDeviceQuery(kustoQuery, deviceId: deviceId);
            if (string.IsNullOrEmpty(queryId))
            {
                Logger.Error("Failed to get query ID");
                return;
            }

            // Request query results until successful or max retries reached
            string queryResults = await GetDeviceQueryResults(deviceId, queryId, maxRetries: maxRetries, retryDelay: retryDelay);
            if (queryResults is null)
            {
                Logger.Error("Failed to get query results");
                return;
            }
            
            JsonHandler.PrintProperties(queryResults);
            return;
        }

        public async Task<IntuneDevice> GetDevice(string deviceId = "", string deviceName = "", string[] properties = null, 
            IDatabaseHandler database = null)
        {
            List<IntuneDevice> devices = await GetDevices(deviceId, deviceName, properties, database, printJson: false);
            if (devices.Count > 1)
            {
                Logger.Error("Multiple devices found matching the specified device name");
                return null;
            }
            else if (devices.Count == 0)
            {
                Logger.Error($"Failed to find the specified device");
                return null;
            }
            deviceId = devices.FirstOrDefault()?.Properties["id"].ToString();
            return devices.FirstOrDefault();
        }

        public async Task<List<IntuneDevice>> GetDevices(string deviceId = "", string deviceName = "", string[] properties = null, 
            IDatabaseHandler database = null, bool printJson = true)
        {
            List<IntuneDevice> intuneDevices = new List<IntuneDevice>();

            // Get all devices by default
            string intuneDevicesUrl = "https://graph.microsoft.com/beta/deviceManagement/manageddevices";

            // Filter to specific devices
            if (!string.IsNullOrEmpty(deviceId))
            {
                intuneDevicesUrl += $"('{deviceId}')";
            }
            else if (!string.IsNullOrEmpty(deviceName))
            {
                intuneDevicesUrl += $"?filter=deviceName%20eq%20%27{deviceName}%27";
            }

            // Request devices from Intune
            Logger.Info("Requesting devices from Intune");
            HttpResponseMessage devicesResponse = await _graphClient._httpHandler.GetAsync(intuneDevicesUrl);
            if (devicesResponse is null)
            {
                Logger.Error("Failed to get devices from Intune");
                return null;
            }

            // Deserialize the JSON response to a dictionary
            string devicesResponseContent = await devicesResponse.Content.ReadAsStringAsync();
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            var deviceResponseDict = serializer.Deserialize<Dictionary<string, object>>(devicesResponseContent);
            if (deviceResponseDict is null) return null;

            var devices = (ArrayList)deviceResponseDict["value"];

            if (devices.Count > 0)
            {
                Logger.Info($"Found {devices.Count} matching {(devices.Count == 1 ? "device" : "devices")} in Intune");
                foreach (Dictionary<string, object> device in devices)
                {
                    // Create an object for each item in the response
                    var intuneDevice = new IntuneDevice(device);
                    intuneDevices.Add(intuneDevice);
                    if (database != null)
                    {
                        // Insert new record if matching id doesn't exist
                        database.Upsert(intuneDevice);
                    }
                }
                if (database != null)
                {
                    Logger.Info($"Upserted {(devices.Count == 1 ? "device" : "devices")} in the database");
                }
                // Convert the devices ArrayList to JSON blob string
                string devicesJson = JsonConvert.SerializeObject(devices, Formatting.Indented);

                // Print the selected properties of the devices
                if (printJson) JsonHandler.PrintProperties(devicesJson, properties);
            }
            else
            {
                Logger.Info("No devices found");
            }
            return intuneDevices;
        }

        public async Task<string> GetDeviceQueryResults(string deviceId, string queryId, int maxRetries, int retryDelay)
        {
            string queryResults = "";
            string url = $"https://graph.microsoft.com/beta/deviceManagement/managedDevices/{deviceId}/queryResults/{queryId}";
            int attempts = 0;

            while (attempts < maxRetries)
            {
                attempts++;
                Logger.Info($"Attempt {attempts} of {maxRetries} to fetch query results");
                HttpResponseMessage response = await _graphClient._httpHandler.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    if (responseContent.Contains("successful"))
                    {
                        Logger.Info($"Successfully fetched query results on attempt {attempts}:");
                        string encodedQueryResults = StringHandler.GetMatch(responseContent, "\"results\":\"([^\"]+)\"");
                        queryResults = Encoding.UTF8.GetString(Convert.FromBase64String(encodedQueryResults));
                        break;
                    }
                    else
                    {
                        Logger.Info($"Query results not yet available, retrying in {retryDelay} seconds");
                        await Task.Delay(retryDelay * 1000);
                    }
                }
            }

            if (string.IsNullOrEmpty(queryResults))
            {
                Logger.Error($"Failed to fetch query results after {maxRetries} attempts");
            }

            return queryResults;
        }

        public async Task<List<IntuneScript>> GetScripts(string scriptId = "", string[] properties = null, IDatabaseHandler database = null, bool printJson = true)
        {
            List<IntuneScript> intuneScripts = new List<IntuneScript>();

            Logger.Info($"Requesting scripts from Intune");
            string url = $"https://graph.microsoft.com/beta/deviceManagement/deviceHealthScripts";
            if (!string.IsNullOrEmpty(scriptId))
            {
                url += $"/{scriptId}";
            }
            HttpResponseMessage scriptsResponse = await _graphClient._httpHandler.GetAsync(url);
            if (scriptsResponse.StatusCode != HttpStatusCode.OK)
            {
                Logger.Error("Failed to get scripts from Intune");
                return intuneScripts;
            }

            // Deserialize the JSON response to a dictionary
            string scriptsResponseContent = await scriptsResponse.Content.ReadAsStringAsync();
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            var scriptsResponseDict = serializer.Deserialize<Dictionary<string, object>>(scriptsResponseContent);
            if (scriptsResponseDict is null) return null;

            var scripts = (ArrayList)scriptsResponseDict["value"];

            if (scripts.Count > 0)
            {
                Logger.Info($"Found {scripts.Count} {(scripts.Count == 1 ? "script" : "scripts")} in Intune");
                foreach (Dictionary<string, object> script in scripts)
                {
                    // Create an object for each item in the response
                    var intuneScript = new IntuneScript(script);
                    intuneScripts.Add(intuneScript);
                    if (database != null)
                    {
                        // Insert new record if matching id doesn't exist
                        database.Upsert(intuneScript);
                    }
                }
                if (database != null)
                {
                    Logger.Info("Upserted scripts in the database");
                }
                // Convert the devices ArrayList to JSON blob string
                string scriptsJson = JsonConvert.SerializeObject(scripts, Formatting.Indented);

                // Print the selected properties of the devices
                if (printJson) JsonHandler.PrintProperties(scriptsJson, properties);
            }
            else
            {
                Logger.Info("No scripts found");
            }
            return intuneScripts;
        }

        public async Task InitiateOnDemandProactiveRemediation(string deviceId, string scriptId)
        {
            Logger.Info($"Initiating on demand proactive remediation - execution script {scriptId} on device {deviceId}");
            string url =
                $"https://graph.microsoft.com/beta/deviceManagement/managedDevices('{deviceId}')/initiateOnDemandProactiveRemediation";
            var content = _graphClient._httpHandler.CreateJsonContent(new
            {
                ScriptPolicyId = scriptId,
            });
            await _graphClient._httpHandler.PostAsync(url);
        }

        public async Task<string> NewDeviceAssignmentFilter(string deviceName)
        {
            string displayName = Guid.NewGuid().ToString();
            Logger.Info($"Creating new device assignment filter with displayName: {displayName}");

            string url = "https://graph.microsoft.com/beta/deviceManagement/assignmentFilters";
            var content = _graphClient._httpHandler.CreateJsonContent(new
            {
                displayName,
                description = "",
                platform = "Windows10AndLater",
                rule = $"(device.deviceName -eq \"{deviceName}\")",
                roleScopeTagIds = new List<string> { "0" }
            });
            HttpResponseMessage response = await _graphClient._httpHandler.PostAsync(url, content);
            if (response.StatusCode != HttpStatusCode.Created)
            {
                Logger.Error("Failed to create device assignment filter");
                return null;
            }

            string responseContent = await response.Content.ReadAsStringAsync();
            string filterId = StringHandler.GetMatch(responseContent, "\"id\":\"([^\"]+)\"");
            Logger.Info($"Obtained filter ID: {filterId}");
            return filterId;
        }

        public async Task NewDeviceManagementScriptAssignment(string filterId, string scriptId)
        {
            // Start script 5 minutes from now to account for sync
            var now = DateTime.UtcNow.AddMinutes(5);
            Logger.Info($"Assigning script {scriptId} to filter {filterId}");
            string url = $"https://graph.microsoft.com/beta/deviceManagement/deviceHealthScripts/{scriptId}/assign";

            var content = _graphClient._httpHandler.CreateJsonContent(new
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
            await _graphClient._httpHandler.PostAsync(url, content);
        }

        public async Task NewDeviceManagementScriptAssignmentHourly(string filterId, string scriptId)
        {
            // Start script 5 minutes from now to account for sync
            var now = DateTime.UtcNow.AddMinutes(5);
            Logger.Info($"Assigning script {scriptId} to filter {filterId}");
            string url = $"https://graph.microsoft.com/beta/deviceManagement/deviceHealthScripts/{scriptId}/assign";

            var content = _graphClient._httpHandler.CreateJsonContent(new
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
            await _graphClient._httpHandler.PostAsync(url, content);
        }

        public async Task<string> NewDeviceQuery(string query, string deviceId = "", string deviceName = "", IDatabaseHandler database = null)
        {
            string queryId = "";
            Logger.Info($"Executing device query: {query}");
            if (!string.IsNullOrEmpty(deviceId))
            {
                string url = $"https://graph.microsoft.com/beta/deviceManagement/managedDevices/{deviceId}/createQuery";
                string encodedQuery = Convert.ToBase64String(Encoding.UTF8.GetBytes(query));
                var content = _graphClient._httpHandler.CreateJsonContent(new
                {
                    query = encodedQuery
                });
                HttpResponseMessage response = await _graphClient._httpHandler.PostAsync(url, content);
                if (!response.IsSuccessStatusCode)
                {
                    Logger.Error("Failed to execute query");
                    return null;
                }
                string responseContent = await response.Content.ReadAsStringAsync();
                queryId = StringHandler.GetMatch(responseContent, "\"id\":\"([^\"]+)\"");
                Logger.Info($"Obtained query ID: {queryId}");
            }
            else if (!string.IsNullOrEmpty(deviceName))
            {
                throw new NotImplementedException();
            }
            return queryId;
        }

        public async Task<string> NewScriptPackage(string displayName, string detectionScriptContent, string description = "", string publisher = "", string remediationScriptContent = "", bool runAs32Bit = true, string runAsAccount = "system")
        {
            Logger.Info($"Creating new detection script with displayName: {displayName}");
            string url = "https://graph.microsoft.com/beta/deviceManagement/deviceHealthScripts";

            var content = _graphClient._httpHandler.CreateJsonContent(new
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
            HttpResponseMessage response = await _graphClient._httpHandler.PostAsync(url, content);
            if (response.StatusCode != HttpStatusCode.Created)
            {
                Logger.Error("Failed to create detection script");
                return null;
            }
            string responseContent = await response.Content.ReadAsStringAsync();
            string scriptId = StringHandler.GetMatch(responseContent, "\"id\":\"([^\"]+)\"");
            Logger.Info($"Obtained script ID: {scriptId}");
            return scriptId;
        }

        public List<IntuneDevice> ShowIntuneDevices(IDatabaseHandler database, string[] properties, string deviceId = "",
            string deviceName = "")
        {
            List<IntuneDevice> intuneDevices = new List<IntuneDevice>();

            if (!string.IsNullOrEmpty(deviceId))
            {
                var device = database.FindByPrimaryKey<IntuneDevice>(deviceId);
                if (device != null)
                {
                    Logger.Info($"Found a matching device in the database");
                    JsonHandler.PrintProperties(device.ToString(), properties);
                    Dictionary<string, object> deviceProperties = BsonDocumentHandler.ToDictionary(device);
                    intuneDevices.Add(new IntuneDevice(deviceProperties));
                }
                else
                {
                    Logger.Info("No matching device found in the database");
                }
            }
            else if (!string.IsNullOrEmpty(deviceName))
            {

                var databaseDevices = database.FindInCollection<IntuneDevice>("deviceName", deviceName);
                if (databaseDevices.Any())
                {
                    Logger.Info($"Found {databaseDevices.Count()} matching devices in the database");
                    foreach (var device in databaseDevices)
                    {
                        JsonHandler.PrintProperties(device.ToString(), properties);
                        Dictionary<string, object> deviceProperties = BsonDocumentHandler.ToDictionary(device);
                        intuneDevices.Add(new IntuneDevice(deviceProperties));
                    }
                }
                else
                {
                    Logger.Info("No matching devices found in the database");
                }
            }
            else
            {
                var databaseDevices = database.FindInCollection<IntuneDevice>();
                if (databaseDevices.Any())
                {
                    Logger.Info($"Found {databaseDevices.Count()} matching devices in the database");
                    foreach (var device in databaseDevices)
                    {
                        JsonHandler.PrintProperties(device.ToString(), properties);
                        Dictionary<string, object> deviceProperties = BsonDocumentHandler.ToDictionary(device);
                        intuneDevices.Add(new IntuneDevice(deviceProperties));
                    }
                }
                else
                {
                    Logger.Info("No matching devices found in the database");
                }
            }
            return intuneDevices;
        }

        public List<IntuneScript> ShowIntuneScripts(IDatabaseHandler database, string[] properties, string scriptId = "")
        {
            List<IntuneScript> intuneScripts = new List<IntuneScript>();

            if (!string.IsNullOrEmpty(scriptId))
            {
                var script = database.FindByPrimaryKey<IntuneScript>(scriptId);
                if (script != null)
                {
                    Logger.Info($"Found a matching script in the database");
                    JsonHandler.PrintProperties(script.ToString(), properties);
                    Dictionary<string, object> scriptProperties = BsonDocumentHandler.ToDictionary(script);
                    intuneScripts.Add(new IntuneScript(scriptProperties));
                }
                else
                {
                    Logger.Info("No matching script found in the database");
                }
            }
            else
            {
                var databaseScripts = database.FindInCollection<IntuneScript>();
                if (databaseScripts.Any())
                {
                    Logger.Info($"Found {databaseScripts.Count()} matching scripts in the database");
                    foreach (var script in databaseScripts)
                    {
                        JsonHandler.PrintProperties(script.ToString(), properties);
                        Dictionary<string, object> deviceProperties = BsonDocumentHandler.ToDictionary(script);
                        intuneScripts.Add(new IntuneScript(deviceProperties));
                    }
                }
                else
                {
                    Logger.Info("No matching scripts found in the database");
                }
            }
            return intuneScripts;
        }

        public async Task SyncDevice(string deviceId, IDatabaseHandler database, bool skipDeviceLookup = false)
        {
            if (!skipDeviceLookup)
            {
                IntuneDevice device = await GetDevice(deviceId: deviceId, database: database);
                if (device is null)
                {
                    return;
                }
            }
            Logger.Info($"Sending request for Intune to notify to {deviceId} sync");
            string url = $"https://graph.microsoft.com/beta/deviceManagement/managedDevices('{deviceId}')/syncDevice";
            HttpResponseMessage response = await _graphClient._httpHandler.PostAsync(url);
            if (!(response.StatusCode == HttpStatusCode.NoContent))
            {
                Logger.Error($"Failed to send request for device sync notification");
                return;
            }
            Logger.Info("Successfully sent request for device sync notification");
        }
    }
}
