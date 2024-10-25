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
using Newtonsoft.Json.Linq;

namespace Maestro
{
    public class IntuneClient
    {
        private AuthClient _authClient;
        public HttpHandler HttpHandler;
        public IntuneClient()
        {
            _authClient = new AuthClient();
        }

        public static async Task<IntuneClient> InitAndGetAccessToken(CommandLineOptions options, LiteDBHandler database)
        {
            return await InitAndGetAccessToken(database, options.PrtCookie, options.RefreshToken, options.AccessToken,
                options.Reauth, options.PrtMethod);
        }
        public static async Task<IntuneClient> InitAndGetAccessToken(LiteDBHandler database, string providedPrtCookie = "",
            string providedRefreshToken = "", string providedAccessToken = "", bool reauth = false, int prtMethod = 0)
        {
            var intuneClient = new IntuneClient();

            string authRedirectUrl = "https://intune.microsoft.com/signin/idpRedirect.js";
            string delegationTokenUrl = "https://intune.microsoft.com/api/DelegationToken";
            string extensionName = "Microsoft_Intune_DeviceSettings";
            string resourceName = "microsoft.graph";
            string requiredScope = "DeviceManagementConfiguration.ReadWrite.All";
            intuneClient._authClient = await AuthClient.InitAndGetAccessToken(authRedirectUrl, delegationTokenUrl, extensionName,
                resourceName, database, providedPrtCookie, providedRefreshToken, providedAccessToken, reauth, requiredScope,
                prtMethod, accessTokenMethod: 1);

            if (intuneClient._authClient is null)
            {
                Logger.Error("Failed to obtain an access token");
                return null;
            }
            // Copy the HttpHandler from the AuthClient for use in the IntuneClient
            intuneClient.HttpHandler = intuneClient._authClient.HttpHandler;
            return intuneClient;
        }

        // intune apps
        public async Task<List<IntuneApp>> GetApps(string id = "", string displayName = "", string[] properties = null,
            string filter = "", LiteDBHandler database = null, bool printJson = true, bool raw = false)
        {
            Logger.Info($"Requesting apps from Intune");
            string baseUrl = "https://graph.microsoft.com/beta/deviceAppManagement/mobileApps";

            var filters = new List<(string, string, string, string)>();
            filters.Insert(0, ("", "microsoft.graph.managedApp/appAvailability", "eq", "null"));
            filters.Insert(1, ("or", "microsoft.graph.managedApp/appAvailability", "eq", "'lineOfBusiness'"));
            filters.Add(("and", "isAssigned", "eq", "true"));

            if (!string.IsNullOrEmpty(displayName))
            {
                Logger.Warning("Filtering by displayName is not supported for Intune apps, filtering results instead");
            }

            if (!string.IsNullOrEmpty(id))
            {
                baseUrl += $"('{id}')";
            }
            if (!string.IsNullOrEmpty(filter))
            {
                // Split the filter string into three parts: property, operator, value
                filters.Add(("and", filter.Split(' ')[0], filter.Split(' ')[1], filter.Split(' ')[2]));
            }

            List<IntuneApp> apps = await HttpHandler.GetMSGraphEntities<IntuneApp>(
                baseUrl: baseUrl,
                entityCreator: json => new IntuneApp(json, database),
                filters: filters,
                properties: properties,
                database: database,
                printJson: false,
                raw: raw);

            if (apps is null) return null;


            if (!string.IsNullOrEmpty(displayName))
            {
                apps = apps.Where(app => app.Properties["displayName"].ToString() == displayName).ToList();
            }

            Logger.Info($"Found {apps.Count} apps in filtered results");
            if (printJson)
            {
                foreach (IntuneApp app in apps)
                {
                    Logger.InfoTextOnly(app.Properties["jsonBlob"].ToString());
                }
            }

            return apps;
        }

        public async Task<List<IntuneDevice>> GetDevices(string id = "", string deviceName = "", string aadDeviceId = "",
            string[] properties = null, string filter = "", LiteDBHandler database = null, bool printJson = true, bool raw = false)
        {
            Logger.Info($"Requesting devices from Intune");
            string baseUrl = "https://graph.microsoft.com/beta/deviceManagement/manageddevices";

            var filters = new List<(string, string, string, string)>();

            if (!string.IsNullOrEmpty(id))
            {
                baseUrl += $"('{id}')";
            }
            if (!string.IsNullOrEmpty(deviceName))
            {
                filters.Add(("and", "deviceName", "eq", $"'{deviceName}'"));
            }
            if (!string.IsNullOrEmpty(aadDeviceId))
            {
                filters.Add(("and", "azureADDeviceId", "eq", $"'{aadDeviceId}'"));
            }
            if (!string.IsNullOrEmpty(filter))
            {
                // Split the filter string into three parts: property, operator, value
                filters.Add(("and", filter.Split(' ')[0], filter.Split(' ')[1], filter.Split(' ')[2]));
            }

            List<IntuneDevice> devices = await HttpHandler.GetMSGraphEntities<IntuneDevice>(
                baseUrl: baseUrl,
                entityCreator: json => new IntuneDevice(json, database),
                filters: filters,
                properties: properties,
                database: database,
                printJson: printJson,
                raw: raw);

            if (devices is null)
            {
                //Logger.Warning("Found 0 matching devices in Intune");
                return null;
            }
            Logger.Info($"Found {devices.Count} devices in filtered results");
            return devices;
        }

        public async Task<List<IntuneScript>> GetScripts(string id = "", string displayName = "", string[] properties = null,
            string filter = "", LiteDBHandler database = null, bool printJson = true, bool raw = false)
        {
            Logger.Info($"Requesting scripts from Intune");
            string baseUrl = $"https://graph.microsoft.com/beta/deviceManagement/deviceHealthScripts";

            var filters = new List<(string, string, string, string)>();

            if (!string.IsNullOrEmpty(id))
            {
                baseUrl += $"('{id}')";
            }
            if (!string.IsNullOrEmpty(displayName))
            {
                filters.Add(("and", "displayName", "eq", $"'{displayName}'"));
            }
            if (!string.IsNullOrEmpty(filter))
            {
                // Split the filter string into three parts: property, operator, value
                filters.Add(("and", filter.Split(' ')[0], filter.Split(' ')[1], filter.Split(' ')[2]));
            }

            //string expand = "assignments,runSummary";

            List<IntuneScript> scripts = await HttpHandler.GetMSGraphEntities<IntuneScript>(
                baseUrl: baseUrl,
                entityCreator: json => new IntuneScript(json, database),
                filters: filters,
                //expand: expand,
                properties: properties,
                database: database,
                printJson: printJson,
                raw: raw);

            if (scripts is null)
            {
                Logger.Warning("Found 0 matching scripts in Intune");
                return null;
            }
            Logger.Info($"Found {scripts.Count} scripts in filtered results");
            return scripts;
        }

        // intune devices
        public async Task<IntuneDevice> GetDevice(string deviceId = "", string deviceName = "", string aadDeviceId = "",
            string[] properties = null, string whereCondition = "", LiteDBHandler database = null)
        {
            List<IntuneDevice> devices = await GetDevices(deviceId, deviceName, aadDeviceId, properties, whereCondition, database, printJson: false);
            if (devices is null) return null;

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

        public List<IntuneDevice> ShowIntuneDevices(LiteDBHandler database, string[] properties, string deviceId = "",
            string deviceName = "")
        {
            List<IntuneDevice> intuneDevices = new List<IntuneDevice>();

            if (!string.IsNullOrEmpty(deviceId))
            {
                var device = database.FindByPrimaryKey<IntuneDevice>(deviceId);
                if (device != null)
                {
                    Logger.Info($"Found a matching device in the database");
                    JsonHandler.GetProperties(device.ToString(), false, properties);
                    Dictionary<string, object> deviceProperties = BsonDocumentHandler.ToDictionary(device);
                    intuneDevices.Add(new IntuneDevice(deviceProperties, database));
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
                        JsonHandler.GetProperties(device.ToString(), false, properties);
                        Dictionary<string, object> deviceProperties = BsonDocumentHandler.ToDictionary(device);
                        intuneDevices.Add(new IntuneDevice(deviceProperties, database));
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
                        JsonHandler.GetProperties(device.ToString(), false, properties);
                        Dictionary<string, object> deviceProperties = BsonDocumentHandler.ToDictionary(device);
                        intuneDevices.Add(new IntuneDevice(deviceProperties, database));
                    }
                }
                else
                {
                    Logger.Info("No matching devices found in the database");
                }
            }
            return intuneDevices;
        }


        // intune exec app
        public async Task<string> ExecWin32App(CommandLineOptions options, LiteDBHandler database)
        {
            string appName = options.Name;
            if (appName is null)
            {
                // Assign a random guid as the app name if not specified
                appName = "app_" + System.Guid.NewGuid();
            }

            // Authenticate and get an access token for EntraID 
            var entraClient = new EntraClient();
            entraClient = await EntraClient.InitAndGetAccessToken(options, database);

            string groupId = "";
            string newGroupId = "";

            // Find the specified group
            if (options.Group != null)
            {
                groupId = options.Group;
                EntraGroup group = await entraClient.GetGroup(groupId, null, null, database);
                if (group is null) return null;
            }

            // Find the specified device
            IntuneDevice intuneDevice = null;
            if (options.Device != null)
            {
                // Check if this is an Intune device ID
                Logger.Info("Checking whether the specified device exists in Intune");
                intuneDevice = await GetDevice(options.Device, database: database);

                // Next, check if this is an Entra device ID or get it from the Intune device object
                if (intuneDevice is null)
                {
                    Logger.Info("Checking whether the specified device exists in Entra");
                    intuneDevice = await GetDevice(aadDeviceId: options.Device, database: database);
                };
                if (intuneDevice is null)
                {
                    Logger.Error("Failed to find the device in Intune or Entra");
                    return null;
                }

                if (intuneDevice.Properties["azureADDeviceId"] == null)
                {
                    Logger.Error("Failed to identify the ID for the device in Entra");
                    return null;
                }

                // Correlate the Entra device ID with the Entra object ID
                string entraDeviceId = intuneDevice.Properties["azureADDeviceId"].ToString();
                EntraDevice entraDevice = await entraClient.GetDevice(deviceDeviceId: entraDeviceId, database: database);
                if (entraDevice is null) return null;

                // Create an Entra group containing the device's Entra object ID
                newGroupId = await entraClient.NewGroup(intuneDevice.Properties["deviceName"].ToString(),
                    entraDevice.Properties["id"].ToString());
                if (newGroupId is null) return null;

                groupId = newGroupId;
            }

            // Find devices in the Entra group
            List<JsonObject> groupMembers = null;
            int attempt = 1;
            int total_attempts = 6;
            while (attempt < total_attempts)
            {
                Logger.Info($"Checking Entra group members every 10 seconds, attempt {attempt} of {total_attempts}");

                groupMembers = await entraClient.GetGroupMembers(groupId, "EntraDevice");
                if (groupMembers == null)
                {
                    await Task.Delay(10000);
                    attempt++;
                }
                else
                {
                    break;
                }
            }

            // If no devices are found after the final attempt, exit
            if (groupMembers.Count == 0)
            {
                Logger.Error("No devices found in the Entra group");
                return null;
            }

            // Run as system by default
            string runAsAccount = "system";
            if (options.AsUser)
            {
                // Run as logged in user if specified
                runAsAccount = "user";
            }

            // Create the app and assign it to the group
            string appId = await NewWin32App(groupId, appName, options.Path, runAsAccount);
            if (appId is null) return null;

            if (!await AssignAppToGroup(appId, groupId)) return null;
            Logger.Info($"App assigned to {groupId}");

            if (options.Device != null)
            {
                Logger.Info("Waiting 30 seconds before requesting device sync");
                await Task.Delay(30000);
                await SyncDevice(intuneDevice.Id, database, skipDeviceLookup: true);
            }

            if (options.Group != null)
            {
                Logger.Info($"Fetching all members of {groupId} for device sync");

                // Populate additional properties for the devices (e.g. deviceId)
                List<EntraDevice> entraDevices = await entraClient.GetDevices(groupMembers, printJson: false);

                // Correlate the Entra device IDs with Intune device IDs
                List<IntuneDevice> intuneDevices = await GetIntuneDevicesFromEntraDevices(entraDevices);

                if (intuneDevices.Count != 0)
                {
                    Logger.Info("Waiting 30 seconds before requesting device sync");
                    await Task.Delay(30000);
                    await SyncDevices(intuneDevices, database);
                }
                else
                {
                    Logger.Error("No devices found in Intune for the Entra group");
                    return null;
                }
            }

            Logger.Info($"App with id {appId} has been deployed");

            string dbString = "";
            if (database?.Path != null)
            {
                dbString = $" -d {database.Path}";
            }
            // Always write the cleanup commands to the console
            Logger.ErrorTextOnly($"\nClean up after execution:\n    .\\Maestro.exe delete intune app -i {appId}{dbString}");

            if (!string.IsNullOrEmpty(newGroupId))
            {
                Logger.ErrorTextOnly($"    .\\Maestro.exe delete entra group -i {groupId}{dbString}");
            }
            Console.WriteLine();
            return appId;
        }


        public async Task<string> NewWin32App(string groupId, string appName, string installationPath, string runAsAccount)
        {
            Logger.Info($"Creating new app with displayName: {appName}");

            string appId = await SaveApplication(appName, installationPath, runAsAccount);
            if (string.IsNullOrEmpty(appId)) return null;

            if (!await CreateAppContentVersion(appId)) return null;

            string contentFileId = await CreateAppContentFile(appId);
            if (string.IsNullOrEmpty(contentFileId)) return null;

            string azureStorageUri = await GetAzureStorageUri(appId, contentFileId);
            if (string.IsNullOrEmpty(azureStorageUri)) return null;

            if (!await PutContentFile(azureStorageUri)) return null;

            if (!await PutContentBlockList(azureStorageUri)) return null;

            if (!await SaveAppContentFileEncryptionInfo(appId, contentFileId)) return null;

            if (!await CommitApp(appId)) return null;

            return appId;
        }

        public async Task<string> SaveApplication(string appName, string installationPath, string runAsAccount)
        {
            Logger.Info("Creating Win32 app");
            string url = "https://graph.microsoft.com/beta/deviceAppManagement/mobileApps/";
            string appData = $@"
            {{
                ""@odata.type"": ""#microsoft.graph.win32LobApp"",
                ""applicableArchitectures"": ""x64"",
                ""allowAvailableUninstall"": false,
                ""categories"": [],
                ""description"": """",
                ""developer"": """",
                ""displayName"": ""{appName}"",
                ""displayVersion"": """",
                ""fileName"": ""{appName}.intunewin"",
                ""installCommandLine"": ""{installationPath.Replace(@"\", @"\\")}"",
                ""installExperience"": {{
                    ""deviceRestartBehavior"": ""suppress"",
                    ""maxRunTimeInMinutes"": 60,
                    ""runAsAccount"": ""{runAsAccount}""
                }},
                ""informationUrl"": """",
                ""isFeatured"": false,
                ""roleScopeTagIds"": [],
                ""notes"": """",
                ""minimumFreeDiskSpaceInMB"": null,
                ""minimumMemoryInMB"": null,
                ""minimumSupportedWindowsRelease"": ""1607"",
                ""msiInformation"": null,
                ""owner"": """",
                ""privacyInformationUrl"": """",
                ""publisher"": """",
                ""returnCodes"": [],
                ""rules"": [
                    {{
                        ""@odata.type"": ""#microsoft.graph.win32LobAppFileSystemRule"",
                        ""ruleType"": ""detection"",
                        ""operator"": ""notConfigured"",
                        ""check32BitOn64System"": false,
                        ""operationType"": ""exists"",
                        ""comparisonValue"": null,
                        ""fileOrFolderName"": ""Mayyhem"",
                        ""path"": ""C:\\""
                    }}
                ],
                ""runAs32Bit"": false,
                ""setupFilePath"": ""{appName}.exe"",
                ""uninstallCommandLine"": ""{installationPath.Replace(@"\", @"\\")}""
            }}";

            // Deserialize the JSON string into a dynamic object
            dynamic appJson = JsonConvert.DeserializeObject<dynamic>(appData);
            string json = JsonConvert.SerializeObject(appJson, Formatting.Indented);

            // Create JSON content from the dynamic object
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Set headers
            content.Headers.Add("X-Ms-Command-Name", "saveApplication");

            // Send the POST request to create the Win32 app
            HttpResponseMessage response = await HttpHandler.PostAsync(url, content);

            if (response.StatusCode != HttpStatusCode.Created)
            {
                Logger.Error($"Failed to create Win32 app: {response.StatusCode} {response.Content}");
                return null;
            }

            string responseContent = await response.Content.ReadAsStringAsync();
            string appId = StringHandler.GetMatch(responseContent, "\"id\":\"([^\"]+)\"");
            Logger.Info($"Obtained app ID: {appId}");
            return appId;
        }

        public async Task<bool> CreateAppContentVersion(string appId)
        {
            Logger.Info("Creating content version for Win32 app");
            string url = $"https://graph.microsoft.com/beta/deviceAppManagement/mobileApps/{appId}" +
                $"/microsoft.graph.win32LobApp/contentVersions";

            string jsonContent = "{}";
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Set headers
            content.Headers.Add("X-Ms-Command-Name", "createLobAppContentVersion");

            HttpResponseMessage response = await HttpHandler.PostAsync(url, content);

            if (response.StatusCode != HttpStatusCode.Created)
            {
                Logger.Error($"Failed to create content version for Win32 app: {response.StatusCode} {response.Content}");
                return false;
            }

            return true;
        }

        public async Task<string> CreateAppContentFile(string appId)
        {
            Logger.Info("Creating content file for Win32 app");
            string url = $"https://graph.microsoft.com/beta/deviceAppManagement/mobileApps/{appId}" +
                $"/microsoft.graph.win32LobApp/contentVersions/1/files";

            string appData = $@"
            {{
                ""name"": ""IntunePackage.intunewin"",
                ""size"": 7269,
                ""sizeEncrypted"": 7328,
                ""manifest"": null,
                ""isDependency"": false,
                ""@odata.type"": ""#microsoft.graph.mobileAppContentFile""
            }}";

            // Deserialize the JSON string into a dynamic object
            dynamic appJson = JsonConvert.DeserializeObject<dynamic>(appData);
            string json = JsonConvert.SerializeObject(appJson, Formatting.Indented);

            // Create JSON content from the dynamic object
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Set headers
            content.Headers.Add("X-Ms-Command-Name", "createLobAppContentFile");

            HttpResponseMessage response = await HttpHandler.PostAsync(url, content);

            if (response.StatusCode != HttpStatusCode.Created)
            {
                Logger.Error($"Failed to create content file for Win32 app: {response.StatusCode} {response.Content}");
                return null;
            }

            string responseContent = await response.Content.ReadAsStringAsync();
            string contentFileId = StringHandler.GetMatch(responseContent, "\"id\":\"([^\"]+)\"");
            Logger.Info($"Obtained content file ID: {contentFileId}");
            return contentFileId;
        }

        public async Task<string> GetAzureStorageUri(string appId, string contentFileId)
        {
            Logger.Info("Requesting Azure storage URI for Win32 app");
            string url = $"https://graph.microsoft.com/beta/deviceAppManagement/mobileApps/{appId}" +
                $"/microsoft.graph.win32LobApp/contentVersions/1/files/{contentFileId}";

            bool successful = false;
            int tries = 0;
            int maxRequests = 5;
            int retryDelay = 3;
            string azureStorageUrl = "";

            while (!successful && tries < maxRequests)
            {
                Logger.Info($"Attempt {tries + 1} of {maxRequests} to get azureStorageUri for content file ID");

                HttpHandler.SetHeader("X-Ms-Command-Name", "_retryGetLobAppContentFileRequest");
                HttpResponseMessage response = await HttpHandler.GetAsync(url);
                HttpHandler.RemoveHeader("X-Ms-Command-Name");
                string responseContent = await response.Content.ReadAsStringAsync();
                azureStorageUrl = StringHandler.GetMatch(responseContent, "\"azureStorageUri\":\"([^\"]+)\"");
                if (!string.IsNullOrEmpty(azureStorageUrl))
                {
                    successful = true;
                    Logger.Info($"Obtained Azure storage URI: {azureStorageUrl}");
                    return azureStorageUrl;
                }
                await Task.Delay(retryDelay * 1000);
                tries++;
            }
            Logger.Error("Failed to get Azure storage URI for Win32 app");
            return azureStorageUrl;
        }

        public async Task<bool> PutContentFile(string azureStorageUri)
        {
            Logger.Info("Uploading content file to Azure storage");

            // block-00000000
            string url = $"{azureStorageUri}&comp=block&blockid=YmxvY2stMDAwMDAwMDA=";

            // Base64-encoded calc.exe wrapped with IntuneWinAppUtil.exe to .intunewin format for known good dummy data -- do you trust me?
            // Used burp comparer to copy bytes from valid upload and decoder to convert hex to base64
            string intuneWinFile = "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";
            byte[] winFileBytes = Convert.FromBase64String(intuneWinFile);

            // Trim the last byte (0a) from the byte array that is added during conversion
            winFileBytes = winFileBytes.Take(winFileBytes.Length - 1).ToArray();

            // Upload the content file to Azure storage
            var content = new ByteArrayContent(winFileBytes);

            // Don't include authorization header for this request
            HttpHandler httpHandler = new HttpHandler();
            HttpResponseMessage response = await httpHandler.PutAsync(url, content);

            if (response.StatusCode != HttpStatusCode.Created)
            {
                Logger.Error($"Failed to upload content file to Azure storage: {response.StatusCode} {response.Content}");
                return false;
            }

            Logger.Verbose("Successfully uploaded content file to Azure storage");
            return true;
        }

        public async Task<bool> PutContentBlockList(string azureStorageUri)
        {
            Logger.Info("Sending content block list to Azure storage");
            string url = $"{azureStorageUri}&comp=blocklist";
            string data = $@"<?xml version=""1.0"" encoding=""utf-8""?><BlockList><Latest>YmxvY2stMDAwMDAwMDA=</Latest></BlockList>";

            // Create JSON content from the dynamic object
            var content = new StringContent(data, Encoding.UTF8, "text/plain");

            // Don't include authorization header for this request either
            HttpHandler httpHandler = new HttpHandler();
            HttpResponseMessage response = await httpHandler.PutAsync(url, content);
            if (response.StatusCode != HttpStatusCode.Created)
            {
                Logger.Error($"Failed to send block list: {response.StatusCode} {response.Content}");
                return false;
            }
            Logger.Verbose("Successfully sent content block list to Azure storage");
            return true;
        }

        public async Task<bool> SaveAppContentFileEncryptionInfo(string appId, string contentFileId)
        {
            Logger.Info("Saving file encryption info");
            string url = $"https://graph.microsoft.com/beta/deviceAppManagement/mobileApps/{appId}" +
                $"/microsoft.graph.win32LobApp/contentVersions/1/files/{contentFileId}/commit";

            // Hardcoded values from .intunewin file created for calc.exe using IntuneWinAppUtil.exe
            var content = HttpHandler.CreateJsonContent(new
            {
                fileEncryptionInfo = new {
                    encryptionKey = "OU22tJ9vWbw9Gdj3iLFbFPTyYvNe1/fwzzvd1YzBI/M=",
                    initializationVector = "o9TQazwkEY2uVWHuI+qHFQ==",
                    mac = "OJiQKIDluz9gypatflYsSzxalQbJgOLKDZP+C+hP3dA=",
                    macKey = "6Dt2sAqvXy1baGjdr4s+ivhH5lppmZ83LwucDFQ20L0=",
                    profileIdentifier = "ProfileVersion1",
                    fileDigest = "RVLextu9ni633iqW54ktzkU4kTDgekRFY8ao9gSwM78=",
                    fileDigestAlgorithm = "SHA256"
                }
            });

            content.Headers.Add("X-Ms-Command-Name", "saveLobAppContentFile");
            HttpResponseMessage response = await HttpHandler.PostAsync(url, content);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                Logger.Error($"Failed to save file encryption info: {response.StatusCode} {response.Content}");
                return false;
            }

            Logger.Verbose("Successfully saved file encryption info");
            return true;
        }

        public async Task<bool> CommitApp(string appId)
        {
            Logger.Info("Committing Win32 app");
            string url = $"https://graph.microsoft.com/beta/deviceAppManagement/mobileApps/{appId}";
            string data = $@"
            {{
                ""@odata.type"": ""#microsoft.graph.win32LobApp"",
                ""committedContentVersion"": ""1""
            }}";

            // Deserialize the JSON string into a dynamic object
            dynamic dataJson = JsonConvert.DeserializeObject<dynamic>(data);
            string json = JsonConvert.SerializeObject(dataJson, Formatting.None);

            bool successful = false;
            int tries = 0;
            int maxRequests = 5;
            int retryDelay = 3;

            while (!successful && tries < maxRequests)
            {
                Logger.Info($"Attempt {tries + 1} of {maxRequests} to commit app");
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                content.Headers.Add("X-Ms-Command-Name", "patchLobApp");

                HttpResponseMessage response = await HttpHandler.PatchAsync(url, content);
                
                if (response.StatusCode == HttpStatusCode.NoContent)
                {
                    successful = true;
                    Logger.Info("Successfully committed app");
                    return true;
                }
                await Task.Delay(retryDelay * 1000);
                tries++;
            }
            Logger.Error("Failed to commit app");
            return false;
        }

        public async Task<bool> AssignAppToGroup(string appId, string groupId)
        {
            Logger.Info($"Assigning app to group: {groupId}");
            string url = $"https://graph.microsoft.com/beta/deviceAppManagement/mobileApps/{appId}/assign";
            string data = $@"
            {{
                ""mobileAppAssignments"": 
                [
                    {{
                        ""@odata.type"": ""#microsoft.graph.mobileAppAssignment"",
                        ""target"": {{
                            ""@odata.type"": ""#microsoft.graph.groupAssignmentTarget"",
                            ""groupId"": ""{groupId}""
                        }},
                        ""intent"": ""Required"",
                        ""settings"": 
                        {{
                            ""@odata.type"": ""#microsoft.graph.win32LobAppAssignmentSettings"",
                            ""notifications"": ""hideAll"",
                            ""installTimeSettings"": null,
                            ""restartSettings"": null,
                            ""deliveryOptimizationPriority"": ""notConfigured""
                        }}
                    }}
                ]
            }}";

            // Deserialize the JSON string into a dynamic object
            dynamic dataJson = JsonConvert.DeserializeObject<dynamic>(data);
            string json = JsonConvert.SerializeObject(dataJson, Formatting.None);

            // Create JSON content from the dynamic object
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            content.Headers.Add("X-Ms-Command-Name", "saveGroupTargetingAssignments");

            HttpResponseMessage response = await HttpHandler.PostAsync(url, content);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                Logger.Error($"Failed to assign app: {response.StatusCode} {response.Content}");
                return false;
            }
            Logger.Verbose("Successfully assigned app to group");
            return true;
        }

        public async Task<bool> DeleteApplication(string appId)
        {
            Logger.Info($"Deleting Win32 app: {appId}");
            string url = $"https://graph.microsoft.com/beta/deviceAppManagement/mobileApps/{appId}";

            HttpHandler.SetHeader("X-Ms-Command-Name", "deleteApplication");
            HttpResponseMessage response = await HttpHandler.DeleteAsync(url);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                Logger.Error($"Failed to delete application: {response.StatusCode} {response.Content}");
                return false;
            }

            Logger.Info("Successfully deleted application");
            return true;
        }

        public async Task<List<IntuneDevice>> GetIntuneDevicesFromEntraDevices(List<EntraDevice> entraDevices)
        {
            // Correlate the Entra device IDs with Intune device IDs
            List<IntuneDevice> intuneDevices = new List<IntuneDevice>();

            foreach (EntraDevice entraDevice in entraDevices)
            {
                if (entraDevice.Properties.TryGetValue("deviceId", out object entraDeviceId))
                {
                    if (entraDeviceId is null)
                    {
                        Logger.Warning($"No Intune device ID found for Entra device {entraDevice.Properties["id"]}");
                        continue;
                    }
                    IntuneDevice intuneDevice = await GetDevice(aadDeviceId: entraDeviceId.ToString());
                    if (intuneDevice is null)
                    {
                        Logger.Warning($"No Intune device found for Entra deviceId {entraDeviceId}");
                        continue;
                    }
                    Logger.Info($"Found device {intuneDevice.Properties["id"]} in Intune for Entra device {entraDeviceId}");
                    intuneDevices.Add(intuneDevice);
                }
                if (entraDevice.Properties.TryGetValue("deviceName", out object entraDeviceName))
                {
                    if (entraDeviceName is null)
                    {
                        Logger.Warning($"No Intune device name found for Entra device {entraDevice.Properties["id"]}");
                        continue;
                    }
                    IntuneDevice intuneDevice = await GetDevice(deviceName: entraDeviceName.ToString());
                    if (intuneDevice is null)
                    {
                        Logger.Warning($"No Intune device found for Entra deviceName {entraDeviceName}");
                        continue;
                    }
                    Logger.Info($"Found device {intuneDevice.Properties["id"]} in Intune for Entra device {entraDeviceName}");
                    intuneDevices.Add(intuneDevice);
                }
                else
                {
                    
                }
            }
            return intuneDevices;
        }


        // intune exec query
        public async Task ExecuteDeviceQuery(string kustoQuery, int maxRetries, int retryDelay, string deviceId = "", string deviceName = "", LiteDBHandler database = null)
        {
            // Check whether the device exists in Intune
            IntuneDevice device = await GetDevice(deviceId, deviceName, database: database);
            if (device is null) return;

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

            JsonHandler.GetProperties(queryResults);
            return;
        }

        public async Task<string> NewDeviceQuery(string query, string deviceId = "", string deviceName = "", LiteDBHandler database = null)
        {
            string queryId = "";
            Logger.Info($"Executing device query: {query}");
            if (!string.IsNullOrEmpty(deviceId))
            {
                string url = $"https://graph.microsoft.com/beta/deviceManagement/managedDevices/{deviceId}/createQuery";
                string encodedQuery = Convert.ToBase64String(Encoding.UTF8.GetBytes(query));
                var content = HttpHandler.CreateJsonContent(new
                {
                    query = encodedQuery
                });
                HttpResponseMessage response = await HttpHandler.PostAsync(url, content);
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

        public async Task<string> GetDeviceQueryResults(string deviceId, string queryId, int maxRetries, int retryDelay)
        {
            string queryResults = "";
            string url = $"https://graph.microsoft.com/beta/deviceManagement/managedDevices/{deviceId}/queryResults/{queryId}";
            int attempts = 0;

            while (attempts < maxRetries)
            {
                attempts++;
                Logger.Info($"Attempt {attempts} of {maxRetries} to fetch query results");
                HttpResponseMessage response = await HttpHandler.GetAsync(url);
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


        // intune exec script
        public async Task<string> NewScriptPackage(string displayName, string detectionScriptContent, string description = "", string publisher = "", string remediationScriptContent = "", bool runAs32Bit = true, string runAsAccount = "system")
        {
            Logger.Info($"Creating new detection script with displayName: {displayName}");
            string url = "https://graph.microsoft.com/beta/deviceManagement/deviceHealthScripts";

            var content = HttpHandler.CreateJsonContent(new
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
            HttpResponseMessage response = await HttpHandler.PostAsync(url, content);
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

        public async Task<string> NewDeviceAssignmentFilter(string deviceName)
        {
            string displayName = Guid.NewGuid().ToString();
            Logger.Info($"Creating new device assignment filter with displayName: {displayName}");

            string url = "https://graph.microsoft.com/beta/deviceManagement/assignmentFilters";
            var content = HttpHandler.CreateJsonContent(new
            {
                displayName,
                description = "",
                platform = "Windows10AndLater",
                rule = $"(device.deviceName -eq \"{deviceName}\")",
                roleScopeTagIds = new List<string> { "0" }
            });
            HttpResponseMessage response = await HttpHandler.PostAsync(url, content);
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

            var content = HttpHandler.CreateJsonContent(new
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
            await HttpHandler.PostAsync(url, content);
        }

        public async Task NewDeviceManagementScriptAssignmentHourly(string filterId, string scriptId)
        {
            // Start script 5 minutes from now to account for sync
            var now = DateTime.UtcNow.AddMinutes(5);
            Logger.Info($"Assigning script {scriptId} to filter {filterId}");
            string url = $"https://graph.microsoft.com/beta/deviceManagement/deviceHealthScripts/{scriptId}/assign";

            var content = HttpHandler.CreateJsonContent(new
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
            await HttpHandler.PostAsync(url, content);
        }

        public async Task<HttpResponseMessage> InitiateOnDemandProactiveRemediation(string deviceId, string scriptId)
        {
            Logger.Info($"Executing on demand proactive remediation script {scriptId} on device {deviceId}");
            string url =
                $"https://graph.microsoft.com/beta/deviceManagement/managedDevices('{deviceId}')/initiateOnDemandProactiveRemediation";

            var content = HttpHandler.CreateJsonContent(new
            {
                ScriptPolicyId = scriptId,
            });

            return await HttpHandler.PostAsync(url, content); 
        }

        public async Task<bool> CheckWhetherProactiveRemediationScriptExecuted(string deviceId, int timeout, int retryDelay)
        {
            Logger.Info($"Checking script execution status for device {deviceId} every {retryDelay} seconds");
            string url = $"https://graph.microsoft.com/beta/deviceManagement/managedDevices('{deviceId}')?$select=deviceactionresults";

            bool successful = false;
            int tries = 0;
            string maxRequests = (timeout == 0) ? "∞" : (timeout / retryDelay).ToString();

            if (timeout == 0)
            {
                Logger.Info("Unlimited timeout specified, trying forever");
            }

            while (!successful && (tries < timeout / retryDelay || timeout == 0))
            {
                Logger.Info($"Attempt {tries + 1} of {maxRequests}");
                HttpHandler.SetHeader("X-Ms-Command-Name", "fetchMDMDeviceActionResults");
                HttpResponseMessage response = await HttpHandler.GetAsync(url);
                HttpHandler.RemoveHeader("X-Ms-Command-Name");
                string responseContent = await response.Content.ReadAsStringAsync();

                // Deserialize the JSON response to a dictionary
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                var fullResponseDict = serializer.Deserialize<Dictionary<string, object>>(responseContent);
                if (fullResponseDict == null) return false;

                var deviceActionResults = (ArrayList)fullResponseDict["deviceActionResults"];
                if (deviceActionResults.Count == 0) return false;

                foreach (Dictionary<string, object> deviceActionResult in deviceActionResults)
                {
                    if (deviceActionResult["actionName"].ToString() == "initiateOnDemandProactiveRemediation")
                    {
                        if (deviceActionResult["actionState"].ToString() == "done")
                        {
                            Logger.Info($"The proactive remediation script was executed on {deviceId}");
                            return true;
                        }
                    }
                }
                
                await Task.Delay(retryDelay * 1000);
                tries++;
            }

            Logger.Info($"The proactive remediation script was not executed within the specified timeout period");
            return false;
        }

        public async Task<Dictionary<string, object>> GetScriptOutput(string deviceId, string scriptId, int timeout, int retryDelay,
            string[] properties = null, string filter = "", LiteDBHandler database = null, bool printJson = true, bool raw = false)
        {
            Logger.Info($"Checking script output for device {deviceId} every {retryDelay} seconds");
            string url = $"https://graph.microsoft.com/beta/deviceManagement/managedDevices/{deviceId}/deviceHealthScriptStates?";

            bool successful = false;
            int tries = 0;
            string maxRequests = (timeout == 0) ? "∞" : (timeout / retryDelay).ToString();

            if (timeout == 0)
            {
                Logger.Info("Unlimited timeout specified, trying forever");
            }

            while (!successful && (tries < timeout / retryDelay || timeout == 0))
            {
                Logger.Info($"Attempt {tries + 1} of {maxRequests}");

                HttpHandler.SetHeader("X-Ms-Command-Name", "getDeviceHealthScriptPolicyStates");

                HttpResponseMessage response = await HttpHandler.GetAsync(url);
                HttpHandler.RemoveHeader("X-Ms-Command-Name");
                string responseContent = await response.Content.ReadAsStringAsync();

                // Deserialize the JSON response to a dictionary
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                var fullResponseDict = serializer.Deserialize<Dictionary<string, object>>(responseContent);
                if (fullResponseDict == null) return null;

                var deviceHealthScriptStates = (ArrayList)fullResponseDict["value"];
                if (deviceHealthScriptStates.Count == 0) return null;

                foreach (Dictionary<string, object> deviceHealthScriptState in deviceHealthScriptStates)
                {
                    if (deviceHealthScriptState["policyId"].ToString() == scriptId)
                    {
                        IntuneScriptState intuneScriptState = new IntuneScriptState(deviceHealthScriptState, database);

                        if (deviceHealthScriptState["detectionState"].ToString() == "success")
                        {
                            Logger.Info($"The proactive remediation script was executed on {deviceId}");
                            if (printJson)
                            {
                                Logger.ErrorTextOnly(intuneScriptState.ToString());
                            }
                            else
                            {
                                Logger.ErrorTextOnly(
                                    $"\nFirst line of stdout:\n    {deviceHealthScriptState["preRemediationDetectionScriptOutput"]}\n" +
                                    $"\nFirst line of stderr:\n    {deviceHealthScriptState["preRemediationDetectionScriptError"]}\n");
                            }
                            return deviceHealthScriptState;
                        }
                    }
                }

                await Task.Delay(retryDelay * 1000);
                tries++;
            }

            Logger.Info($"Could not obtain script output within the specified timeout period");
            Logger.Info($"Results should eventually be available at: {url}");
            return null;
        }

        public async Task DeleteDeviceAssignmentFilter(string filterId)
        {

            Logger.Info($"Deleting device assignment filter with ID: {filterId}");
            string url = $"https://graph.microsoft.com/beta/deviceManagement/assignmentFilters/{filterId}";
            await HttpHandler.DeleteAsync(url);
        }

        public async Task DeleteScriptPackage(string scriptId)
        {
            Logger.Info($"Deleting detection script with scriptId: {scriptId}");
            string url = $"https://graph.microsoft.com/beta/deviceManagement/deviceHealthScripts/{scriptId}";
            HttpResponseMessage response = await HttpHandler.DeleteAsync(url);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                Logger.Error("Failed to delete detection script");
                return;
            }
            Logger.Info("Successfully deleted detection script");
        }

 
        // intune scripts
        public List<IntuneScript> ShowIntuneScripts(LiteDBHandler database, string[] properties, string scriptId = "")
        {
            List<IntuneScript> intuneScripts = new List<IntuneScript>();

            if (!string.IsNullOrEmpty(scriptId))
            {
                var script = database.FindByPrimaryKey<IntuneScript>(scriptId);
                if (script != null)
                {
                    Logger.Info($"Found a matching script in the database");
                    JsonHandler.GetProperties(script.ToString(), false, properties);
                    Dictionary<string, object> scriptProperties = BsonDocumentHandler.ToDictionary(script);
                    intuneScripts.Add(new IntuneScript(scriptProperties, database));
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
                        JsonHandler.GetProperties(script.ToString(), false, properties);
                        Dictionary<string, object> deviceProperties = BsonDocumentHandler.ToDictionary(script);
                        intuneScripts.Add(new IntuneScript(deviceProperties, database));
                    }
                }
                else
                {
                    Logger.Info("No matching scripts found in the database");
                }
            }
            return intuneScripts;
        }

        // intune sync
        public async Task SyncDevice(string deviceId, LiteDBHandler database, bool skipDeviceLookup = false)
        {
            if (!skipDeviceLookup)
            {
                IntuneDevice device = await GetDevice(deviceId: deviceId, database: database);
                if (device is null)
                {
                    return;
                }
            }
            Logger.Info($"Sending request for Intune to notify {deviceId} to sync");
            string url = $"https://graph.microsoft.com/beta/deviceManagement/managedDevices('{deviceId}')/syncDevice";
            HttpResponseMessage response = await HttpHandler.PostAsync(url);
            if (!(response.StatusCode == HttpStatusCode.NoContent))
            {
                Logger.Error($"Failed to send request for device sync notification");
                return;
            }
            Logger.Info("Successfully sent request for device sync notification");
        }

        public async Task SyncDevices(List<IntuneDevice> devices, LiteDBHandler database)
        {
            Logger.Info($"Sending request for Intune to notify devices to sync");
            string url = $"https://graph.microsoft.com/beta/deviceManagement/managedDevices/executeAction";

            var content = HttpHandler.CreateJsonContent(new
            {
                actionName = "syncDevice",
                deviceIds = devices.Select(device => device.Properties["id"]).ToList()
            });
            
            HttpResponseMessage response = await HttpHandler.PostAsync(url, content);
            if (!(response.StatusCode == HttpStatusCode.OK))
            {
                Logger.Error($"Failed to send request for device sync notification");
                return;
            }
            Logger.Info("Successfully sent request for device sync notification");
        }
    }
}
