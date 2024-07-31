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
        private AuthClient _authClient;
        public HttpHandler HttpHandler;
        public IntuneClient() 
        {
            _authClient = new AuthClient();
        }

        public static async Task<IntuneClient> InitAndGetAccessToken(LiteDBHandler database, string prtCookie = "", string bearerToken = "", bool reauth = false, int prtMethod = 0)
        {
            var intuneClient = new IntuneClient();
            string authRedirectUrl = "https://intune.microsoft.com/signin/idpRedirect.js";
            string delegationTokenUrl = "https://intune.microsoft.com/api/DelegationToken";
            string extensionName = "Microsoft_Intune_DeviceSettings";
            string resourceName = "microsoft.graph";
            string requiredScope = "DeviceManagementConfiguration.ReadWrite.All";
            intuneClient._authClient = await AuthClient.InitAndGetAccessToken(authRedirectUrl, delegationTokenUrl, extensionName, 
                resourceName, database, prtCookie, bearerToken, reauth, requiredScope, prtMethod);
            // Copy the HttpHandler from the AuthClient for use in the IntuneClient
            intuneClient.HttpHandler = intuneClient._authClient.HttpHandler;
            return intuneClient;
        }

        // intune devices
        public async Task<IntuneDevice> GetDevice(string deviceId = "", string deviceName = "", string[] properties = null,
    LiteDBHandler database = null)
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
            LiteDBHandler database = null, bool printJson = true)
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
            HttpResponseMessage devicesResponse = await HttpHandler.GetAsync(intuneDevicesUrl);
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

            var devices = new ArrayList();
            if (deviceResponseDict.ContainsKey("value"))
            {
                devices = (ArrayList)deviceResponseDict["value"];
            }
            else
            {
                devices.Add(deviceResponseDict);
            }

            if (devices.Count == 0)
            {
                Logger.Info("No devices found");
                return intuneDevices;
            }

            Logger.Info($"Found {devices.Count} matching {(devices.Count == 1 ? "device" : "devices")} in Intune");
            foreach (Dictionary<string, object> device in devices)
            {
                // Create an object for each item in the response
                var intuneDevice = new IntuneDevice(device, database);
                intuneDevices.Add(intuneDevice);
            }
            if (database != null)
            {
                Logger.Info($"Upserted {(devices.Count == 1 ? "device" : "devices")} in the database");
            }
            // Convert the devices ArrayList to JSON blob string
            string devicesJson = JsonConvert.SerializeObject(devices, Formatting.Indented);

            // Print the selected properties of the devices
            if (printJson) JsonHandler.PrintProperties(devicesJson, properties);

            return intuneDevices;
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
                    JsonHandler.PrintProperties(device.ToString(), properties);
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
                        JsonHandler.PrintProperties(device.ToString(), properties);
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
                        JsonHandler.PrintProperties(device.ToString(), properties);
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
        public async Task<bool> NewWin32App(string groupId, string appName, string installationPath, string runAsAccount)
        {
            Logger.Info($"Creating new app with displayName: {appName}");

            string appId = await SaveApp(appName, installationPath, runAsAccount);
            if (string.IsNullOrEmpty(appId)) return false;

            if (!await CreateAppContentVersion(appId)) return false;

            string contentFileId = await CreateAppContentFile(appId);
            if (string.IsNullOrEmpty(contentFileId)) return false;

            string azureStorageUri = await GetAzureStorageUri(appId, contentFileId);
            if (string.IsNullOrEmpty(azureStorageUri)) return false;

            if (!await PutContentFile(azureStorageUri)) return false;

            if (!await PutContentBlockList(azureStorageUri)) return false;

            if (!await SaveAppContentFileEncryptionInfo(appId, contentFileId)) return false;

            if (!await CommitApp(appId)) return false;

            if (!await AssignAppToGroup(appId, groupId)) return false;

            Logger.Info("Successfully created Win32 app");
            return true;
        }

        public async Task<string> SaveApp(string appName, string installationPath, string runAsAccount)
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
                        ""fileOrFolderName"": ""C:\\"",
                        ""path"": ""Mayyhem""
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

            // Send the POST request to create the Win32 app
            HttpResponseMessage response = await HttpHandler.PostAsync(url, content);

            if (response.StatusCode != HttpStatusCode.Created)
            {
                Logger.Error("Failed to create Win32 app");
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
            var stringContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            HttpResponseMessage contentVersionResponse = await HttpHandler.PostAsync(url, stringContent);

            if (contentVersionResponse.StatusCode != HttpStatusCode.Created)
            {
                Logger.Error("Failed to create content version for Win32 app");
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
                ""isDependency"": false,
                ""@odata.type"": ""#microsoft.graph.mobileAppContentFile""
            }}";

            // Deserialize the JSON string into a dynamic object
            dynamic appJson = JsonConvert.DeserializeObject<dynamic>(appData);
            string json = JsonConvert.SerializeObject(appJson, Formatting.Indented);

            // Create JSON content from the dynamic object
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage contentFileResponse = await HttpHandler.PostAsync(url, content);

            if (contentFileResponse.StatusCode != HttpStatusCode.Created)
            {
                Logger.Error("Failed to create content file for Win32 app");
                return null;
            }

            string responseContent = await contentFileResponse.Content.ReadAsStringAsync();
            string contentFileId = StringHandler.GetMatch(responseContent, "\"id\":\"([^\"]+)\"");
            Logger.Info($"Obtained content file ID: {contentFileId}");
            return contentFileId;
        }

        public async Task<string> GetAzureStorageUri(string appId, string contentFileId)
        {
            Logger.Info("Requesting Azure storage URI for Win32 app");
            string url = $"https://graph.microsoft.com/beta/deviceAppManagement/mobileApps/{appId}" +
                $"/microsoft.graph.win32LobApp/contentVersions/1/files/{contentFileId}";

            HttpResponseMessage response = await HttpHandler.GetAsync(url);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                Logger.Error("Failed to get Azure storage URI for Win32 app");
                return null;
            }

            string responseContent = await response.Content.ReadAsStringAsync();
            string azureStorageUrl = StringHandler.GetMatch(responseContent, "\"azureStorageUri\":\"([^\"]+)\"");
            Logger.Info($"Obtained Azure storage URI: {azureStorageUrl}");
            return azureStorageUrl;
        }

        public async Task<bool> PutContentFile(string azureStorageUri)
        {
            Logger.Info("Uploading content file to Azure storage");

            // block-00000000
            string url = $"{azureStorageUri}&comp=block&blockid=YmxvY2stMDAwMDAwMDA=";

            // Base64-encoded calc.exe wrapped with IntuneWinAppUtil.exe to .intunewin format for known good dummy data -- do you trust me?
            string intuneWinFile = "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eI2ukg+jYUuzkC4qYPATODjbnZ2rJbBRZZGJEdv99oPiQxByOg7xqwgXPXW2+t8qW7uXuO7vIWiuQD1D1mq1QCmsjTttOuo8zEqM0GIE/UrGM/c04Y3QkhfCNOh1yLyCbTOZhvdFKDPOTBOFHtC+zlbDWqyL04lw2ABsqEdCDuuqjMSAvbI11VQiWT2/CZiLayQBlG1hM/emnRzAXx7Quuo7rUQCYph8KoGHYioVlr49ARcvrB+kRYvaZgJ4k/E5GXmd4ZHtXJ5rVu0ALPkNYrl6d3g/vqfZ8D67TJUD6n9fMtj8c3QGrmsYZVBLAwQKAAAAAAAzisZYtv9EEEYDAABGAwAAJwAcAEludHVuZVdpblBhY2thZ2UvTWV0YWRhdGEvRGV0ZWN0aW9uLnhtbCCiGAAooBQAAAAAAAAAAAAAAAAAAAAAAAAAAAA8QXBwbGljYXRpb25JbmZvIHhtbG5zOnhzZD0iaHR0cDovL3d3dy53My5vcmcvMjAwMS9YTUxTY2hlbWEiIHhtbG5zOnhzaT0iaHR0cDovL3d3dy53My5vcmcvMjAwMS9YTUxTY2hlbWEtaW5zdGFuY2UiIFRvb2xWZXJzaW9uPSIxLjguNi4wIj4NCiAgPE5hbWU+Y2FsYy5leGU8L05hbWU+DQogIDxVbmVuY3J5cHRlZENvbnRlbnRTaXplPjcyNjk8L1VuZW5jcnlwdGVkQ29udGVudFNpemU+DQogIDxGaWxlTmFtZT5JbnR1bmVQYWNrYWdlLmludHVuZXdpbjwvRmlsZU5hbWU+DQogIDxTZXR1cEZpbGU+Y2FsYy5leGU8L1NldHVwRmlsZT4NCiAgPEVuY3J5cHRpb25JbmZvPg0KICAgIDxFbmNyeXB0aW9uS2V5PkVtODgzV09mVzlQem9GNnZOV25jNUMvdk1sZm55MVVsbkorc0Z3V2NqM0k9PC9FbmNyeXB0aW9uS2V5Pg0KICAgIDxNYWNLZXk+UDI5dEZjTVRjdHovTUx0OTN0KzlOTnBIeUdzMng1YkljZE9yYk5Da2c4bz08L01hY0tleT4NCiAgICA8SW5pdGlhbGl6YXRpb25WZWN0b3I+YUhiTW9vNXBETEJFdHdXaVpUeFNHQT09PC9Jbml0aWFsaXphdGlvblZlY3Rvcj4NCiAgICA8TWFjPjFLaDVHRU1ibWxIcXpXM3JiaGVVOTduY3dFSXgzTWgvUGd0SGJJdkdpZk09PC9NYWM+DQogICAgPFByb2ZpbGVJZGVudGlmaWVyPlByb2ZpbGVWZXJzaW9uMTwvUHJvZmlsZUlkZW50aWZpZXI+DQogICAgPEZpbGVEaWdlc3Q+dU0wSFlaRk1jS0RSTnlrVElZSnplZ3p6amJxSkp4Nk5wU3lUTEVNaUNIND08L0ZpbGVEaWdlc3Q+DQogICAgPEZpbGVEaWdlc3RBbGdvcml0aG0+U0hBMjU2PC9GaWxlRGlnZXN0QWxnb3JpdGhtPg0KICA8L0VuY3J5cHRpb25JbmZvPg0KPC9BcHBsaWNhdGlvbkluZm8+UEsBAi0ACgAAAAAAM4rGWPOmNSegHAAAoBwAADEAAAAAAAAAAAAAAAAAAAAAAEludHVuZVdpblBhY2thZ2UvQ29udGVudHMvSW50dW5lUGFja2FnZS5pbnR1bmV3aW5QSwECLQAKAAAAAAAzisZYtv9EEEYDAABGAwAAJwAAAAAAAAAAAAAAAAALHQAASW50dW5lV2luUGFja2FnZS9NZXRhZGF0YS9EZXRlY3Rpb24ueG1sUEsFBgAAAAACAAIAtAAAALIgAAAAAA==";
            byte[] winFileBytes = Convert.FromBase64String(intuneWinFile);

            // Upload the content file to Azure storage
            var content = new ByteArrayContent(winFileBytes);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

            // Don't include authorization header for this request
            HttpHandler httpHandler = new HttpHandler();
            HttpResponseMessage response = await httpHandler.PutAsync(url, content);

            if (response.StatusCode != HttpStatusCode.Created)
            {
                Logger.Error("Failed to upload content file to Azure storage");
                return false;
            }

            Logger.Info("Successfully uploaded content file to Azure storage");
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
            HttpResponseMessage contentFileResponse = await httpHandler.PutAsync(url, content);
            if (contentFileResponse.StatusCode != HttpStatusCode.Created)
            {
                Logger.Error("Failed to send block list");
                return false;
            }
            Logger.Info("Successfully sent content block list to Azure storage");
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
                    encryptionKey = "Em883WOfW9PzoF6vNWnc5C/vMlfny1UlnJ+sFwWcj3I=",
                    initializationVector = "aHbMoo5pDLBEtwWiZTxSGA==",
                    mac = "1Kh5GEMbmlHqzW3rbheU97ncwEIx3Mh/PgtHbIvGifM=",
                    macKey = "P29tFcMTctz/MLt93t+9NNpHyGs2x5bIcdOrbNCkg8o=",
                    profileIdentifier = "ProfileVersion1",
                    fileDigest = "uM0HYZFMcKDRNykTIYJzegzzjbqJJx6NpSyTLEMiCH4=",
                    fileDigestAlgorithm = "SHA256"
                }
            });

            HttpResponseMessage response = await HttpHandler.PostAsync(url, content);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                Logger.Error("Failed to save file encryption info");
                return false;
            }

            Logger.Info("Successfully saved file encryption info");
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
            string json = JsonConvert.SerializeObject(dataJson, Formatting.Indented);

            // Create JSON content from the dynamic object
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage commitAppResponse = await HttpHandler.PatchAsync(url, content);
            if (commitAppResponse.StatusCode != HttpStatusCode.NoContent)
            {
                Logger.Error("Failed to commit app");
                return false;
            }
            Logger.Info("Successfully committed Win32 app");
            return true;
        }

        public async Task<bool> AssignAppToGroup(string appId, string groupId)
        {
            Logger.Info($"Assigning app to group:{groupId}");
            string url = $"https://graph.microsoft.com/beta/deviceAppManagement/mobileApps/{appId}/assign";
            string data = $@"
            {{
                ""mobileAppAssignments"": [
                    {{
                        ""@odata.type"": ""#microsoft.graph.mobileAppAssignment"",
                        ""target"": {{
                            ""@odata.type"": ""#microsoft.graph.groupAssignmentTarget"",
                            ""groupId"": ""{groupId}""
                        }},
                        ""intent"": ""Required"",
                        ""settings"": [
                            {{
                                ""@odata.type"": ""#microsoft.graph.win32LobAppAssignmentSettings"",
                                ""notifications"": ""showAll"",
                                ""installTimeSettings"": null,
                                ""restartSettings"": null,
                                ""deliveryOptimizationPriority"": ""notConfigured""
                            }}
                        ]
                    }}
                ]

            }}";

            // Deserialize the JSON string into a dynamic object
            dynamic dataJson = JsonConvert.DeserializeObject<dynamic>(data);
            string json = JsonConvert.SerializeObject(dataJson, Formatting.Indented);

            // Create JSON content from the dynamic object
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage commitAppResponse = await HttpHandler.PostAsync(url, content);
            if (commitAppResponse.StatusCode != HttpStatusCode.OK)
            {
                Logger.Error("Failed to assign app");
                return false;
            }
            Logger.Info("Successfully assigned app to group");
            return true;
        }


            // intune exec query
        public async Task ExecuteDeviceQuery(string kustoQuery, int maxRetries, int retryDelay, string deviceId = "", string deviceName = "", LiteDBHandler database = null)
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

        public async Task InitiateOnDemandProactiveRemediation(string deviceId, string scriptId)
        {
            Logger.Info($"Initiating on demand proactive remediation - execution script {scriptId} on device {deviceId}");
            string url =
                $"https://graph.microsoft.com/beta/deviceManagement/managedDevices('{deviceId}')/initiateOnDemandProactiveRemediation";
            var content = HttpHandler.CreateJsonContent(new
            {
                ScriptPolicyId = scriptId,
            });
            await HttpHandler.PostAsync(url);
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
        public async Task<List<IntuneScript>> GetScripts(string scriptId = "", string[] properties = null, LiteDBHandler database = null, bool printJson = true)
        {
            List<IntuneScript> intuneScripts = new List<IntuneScript>();

            Logger.Info($"Requesting scripts from Intune");
            string url = $"https://graph.microsoft.com/beta/deviceManagement/deviceHealthScripts";
            if (!string.IsNullOrEmpty(scriptId))
            {
                url += $"/{scriptId}";
            }
            HttpResponseMessage scriptsResponse = await HttpHandler.GetAsync(url);
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
                    var intuneScript = new IntuneScript(script, database);
                    intuneScripts.Add(intuneScript);
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

        public List<IntuneScript> ShowIntuneScripts(LiteDBHandler database, string[] properties, string scriptId = "")
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
                        JsonHandler.PrintProperties(script.ToString(), properties);
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
            Logger.Info($"Sending request for Intune to notify to {deviceId} sync");
            string url = $"https://graph.microsoft.com/beta/deviceManagement/managedDevices('{deviceId}')/syncDevice";
            HttpResponseMessage response = await HttpHandler.PostAsync(url);
            if (!(response.StatusCode == HttpStatusCode.NoContent))
            {
                Logger.Error($"Failed to send request for device sync notification");
                return;
            }
            Logger.Info("Successfully sent request for device sync notification");
        }
    }
}
