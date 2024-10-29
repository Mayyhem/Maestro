using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Maestro
{
    // EntraID Microsoft Graph client
    public class EntraClient
    {

        private AuthClient _authClient;
        public HttpHandler HttpHandler;
        public EntraClient(string userAgent = "")
        {
            _authClient = new AuthClient(userAgent);
        }

        public static async Task<EntraClient> InitAndGetAccessToken(CommandLineOptions options, LiteDBHandler database)
        {
            return await InitAndGetAccessToken(database, options.PrtCookie, options.RefreshToken, options.AccessToken,
                options.Reauth, options.PrtMethod, options.UserAgent);
        }

        public static async Task<EntraClient> InitAndGetAccessToken(LiteDBHandler database, string providedPrtCookie = "",
            string providedRefreshToken = "", string providedAccessToken = "", bool reauth = false, int prtMethod = 0, string userAgent = "")
        {
            var entraClient = new EntraClient();
            string authRedirectUrl = "https://portal.azure.com/signin/idpRedirect.js";
            string delegationTokenUrl = "https://portal.azure.com/api/DelegationToken";
            string extensionName = "Microsoft_AAD_IAM";
            string resourceName = "microsoft.graph";
            string requiredScope = "Directory.Read.All";
            entraClient._authClient = await AuthClient.InitAndGetAccessToken(authRedirectUrl, delegationTokenUrl, extensionName,
                resourceName, database, providedPrtCookie, providedRefreshToken, providedAccessToken, reauth, requiredScope,
                prtMethod, accessTokenMethod: 1, userAgent: userAgent);

            if (entraClient._authClient is null)
            {
                Logger.Error("Failed to obtain an access token");
                return null;
            }

            // Copy the HttpHandler from the AuthClient for use in the EntraClient
            entraClient.HttpHandler = entraClient._authClient.HttpHandler;
            return entraClient;
        }

        // add member
        public async Task<HttpResponseMessage> AddGroupMember(string groupId, string memberId)
        {
            Logger.Info($"Adding members to group {groupId}");
            string url = $"https://graph.microsoft.com/v1.0/groups/{groupId}/members/$ref";
            StringContent content = new StringContent($"{{\"@odata.id\": \"https://graph.microsoft.com/v1.0/directoryObjects/{memberId}\"}}");
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            HttpResponseMessage addMemberResponse = await HttpHandler.PostAsync(url, content);
            string addMemberResponseContent = await addMemberResponse.Content.ReadAsStringAsync();

            if (addMemberResponse.StatusCode != HttpStatusCode.NoContent)
            {
                Logger.Error("Failed to add members to group");
                JsonHandler.GetProperties(addMemberResponseContent);
                return null;
            }
            Logger.Info($"Successfully added {memberId} to {groupId}");
            return addMemberResponse;
        }

        // batch endpoint requests
        public async Task<HttpResponseMessage> BatchRequest(string[] requests, string[] requestIds,
            string[] requestUrls, string[] requestMethods,
            Dictionary<string, object>[] requestHeaders, string[] requestBodies)
        {
            string url = "https://graph.microsoft.com/v1.0/$batch";
            StringContent content = HttpHandler.CreateJsonContent(new
            {
                requests = requests.Select((request, index) => new
                {
                    id = requestIds[index],
                    method = requestMethods[index],
                    url = requestUrls[index],
                    headers = requestHeaders[index],
                    body = requestBodies[index]
                }).ToArray()
            });

            HttpResponseMessage batchResponse = await HttpHandler.PostAsync(url, content);
            string batchResponseContent = await batchResponse.Content.ReadAsStringAsync();

            if (batchResponse.StatusCode != HttpStatusCode.OK)
            {
                Logger.Error("Failed to send batch request to Entra");
                JsonHandler.GetProperties(batchResponseContent);
                return null;
            }
            return batchResponse;
        }

        // delete entra group
        public async Task<HttpResponseMessage> DeleteGroup(string groupId)
        {
            Logger.Info($"Deleting group {groupId}");
            string url = $"https://graph.microsoft.com/v1.0/groups/{groupId}";
            HttpResponseMessage deleteGroupResponse = await HttpHandler.DeleteAsync(url);
            string deleteGroupResponseContent = await deleteGroupResponse.Content.ReadAsStringAsync();

            if (deleteGroupResponse.StatusCode != HttpStatusCode.NoContent)
            {
                Logger.Error("Failed to delete group");
                JsonHandler.GetProperties(deleteGroupResponseContent);
                return null;
            }
            Logger.Info($"Successfully deleted group {groupId}");
            return deleteGroupResponse;
        }

        // entra groups
        public async Task<EntraGroup> GetGroup(string groupId = "", string groupName = "", string[] properties = null,
            LiteDBHandler database = null)
        {
            List<EntraGroup> groups = await GetGroups(groupId, properties, database, printJson: false);
            if (groups.Count > 1)
            {
                Logger.Error("Multiple groups found matching the specified name");
                return null;
            }
            else if (groups.Count == 0)
            {
                Logger.Warning($"Failed to find the specified group");
                return null;
            }
            groupId = groups.FirstOrDefault()?.Properties["id"].ToString();
            return groups.FirstOrDefault();
        }

        public async Task<List<EntraGroup>> GetGroups(string groupId = "", string[] properties = null, LiteDBHandler database = null,
            bool printJson = true, bool raw = false)
        {
            Logger.Info($"Requesting groups from Entra");
            List<EntraGroup> entraGroups = new List<EntraGroup>();
            string url = "https://graph.microsoft.com/v1.0/$batch";

            StringContent content = null;
            if (!string.IsNullOrEmpty(groupId))
            {
                content = HttpHandler.CreateJsonContent(new
                {
                    requests = new[]
    {
                    new
                    {
                        id = "SecurityEnabledGroups",
                        method = "GET",
                        url = $"groups?$filter=id eq '{groupId}'",
                        headers = new Dictionary<string, object>()
                    }
                }
                });
            }
            else
            {
                content = HttpHandler.CreateJsonContent(new
                {
                    requests = new[]
                    {
                        new
                        {
                            id = "SecurityEnabledGroups",
                            method = "GET",
                            url = "groups?$filter=securityEnabled eq true",
                            headers = new Dictionary<string, object>()
                        }
                    }
                });
            }

            HttpResponseMessage groupsResponse = await HttpHandler.PostAsync(url, content);
            string groupsResponseContent = await groupsResponse.Content.ReadAsStringAsync();

            if (groupsResponse.StatusCode == HttpStatusCode.NotFound)
            {
                return entraGroups;
            }
            else if (groupsResponse.StatusCode != HttpStatusCode.OK)
            {
                Logger.Error("Failed to get groups from Entra");
                JsonHandler.GetProperties(groupsResponseContent, raw);
                return entraGroups;
            }

            // Deserialize the JSON response to a dictionary
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            var fullResponseDict = serializer.Deserialize<Dictionary<string, object>>(groupsResponseContent);
            if (fullResponseDict == null) return null;

            var responses = (ArrayList)fullResponseDict["responses"];
            if (responses.Count == 0) return null;

            Dictionary<string, object> body = new Dictionary<string, object>();
            foreach (Dictionary<string, object> response in responses)
            {
                if (response["id"].ToString() == "SecurityEnabledGroups")
                {
                    body = (Dictionary<string, object>)response["body"];
                    break;
                }
            }
            if (body.Count == 0) return null;

            var groups = (ArrayList)body["value"];
            if (groups.Count == 0)
            {
                Logger.Info("No groups found in Entra");
                return null;
            }

            Logger.Info($"Found {groups.Count} {(groups.Count == 1 ? "group" : "groups")} in Entra");
            foreach (Dictionary<string, object> group in groups)
            {
                // Create an object for each item in the response
                var entraGroup = new EntraGroup(group, database);
                entraGroups.Add(entraGroup);
            }

            // Convert the groups ArrayList to JSON blob string
            string groupsJson = JsonConvert.SerializeObject(groups, Formatting.Indented);

            // Print the selected properties of the devices
            if (printJson) JsonHandler.GetProperties(groupsJson, false, properties);
            return entraGroups;
        }

        public List<EntraGroup> ShowGroups(LiteDBHandler database, string[] properties, string groupId = "")
        {
            List<EntraGroup> entraGroups = new List<EntraGroup>();

            if (!string.IsNullOrEmpty(groupId))
            {
                var group = database.FindByPrimaryKey<EntraGroup>(groupId);
                if (group != null)
                {
                    Logger.Info($"Found a matching group in the database");
                    JsonHandler.GetProperties(group.ToString(), false, properties);
                    Dictionary<string, object> groupProperties = BsonDocumentHandler.ToDictionary(group);
                    entraGroups.Add(new EntraGroup(groupProperties, database));
                }
                else
                {
                    Logger.Info("No matching group found in the database");
                }
            }
            else
            {
                var databaseGroups = database.FindInCollection<EntraGroup>();
                if (databaseGroups.Any())
                {
                    Logger.Info($"Found {databaseGroups.Count()} matching groups in the database");
                    foreach (var group in databaseGroups)
                    {
                        JsonHandler.GetProperties(group.ToString(), false, properties);
                        Dictionary<string, object> groupProperties = BsonDocumentHandler.ToDictionary(group);
                        entraGroups.Add(new EntraGroup(groupProperties, database));
                    }
                }
                else
                {
                    Logger.Info("No matching groups found in the database");
                }
            }
            return entraGroups;
        }

        // entra devices
        public async Task<EntraDevice> GetDevice(string deviceObjectId = "", string deviceDeviceId = "", string deviceName = "", string[] properties = null,
            LiteDBHandler database = null)
        {
            List<EntraDevice> devices = await GetDevices(null, deviceObjectId, deviceDeviceId, deviceName, properties, database, printJson: false);
            if (devices is null) return null;

            if (devices.Count > 1)
            {
                Logger.Error("Multiple devices found matching the specified device name");
                return null;
            }
            else if (devices.Count == 0)
            {
                Logger.Warning($"Failed to find the specified device");
                return null;
            }
            return devices.FirstOrDefault();
        }

        public async Task<List<EntraDevice>> GetDevices(List<JsonObject> entraGroupMembers = null, string deviceId = "", 
            string deviceDeviceId = "", string deviceName = "", string[] properties = null,
            LiteDBHandler database = null, bool printJson = true)
        {

            var entraDevices = new List<EntraDevice>();

            // Get all devices by default
            string entraDevicesUrl = "https://graph.microsoft.com/v1.0/devices";

            // Filter to specific devices
            if (!string.IsNullOrEmpty(deviceId))
            {
                entraDevicesUrl += $"/{deviceId}";
            }
            else if (!string.IsNullOrEmpty(deviceDeviceId))
            {
                entraDevicesUrl += $"?$filter=deviceId%20eq%20%27{deviceDeviceId}%27";
            }
            else if (!string.IsNullOrEmpty(deviceName))
            {
                entraDevicesUrl += $"?$filter=displayName%20eq%20%27{deviceName}%27";
            }
            else if (entraGroupMembers != null)
            {
                var deviceIds = entraGroupMembers.Where(member => member.GetType().Name == "EntraDevice").Select(member => member.Properties["id"].ToString()).ToArray();
                if (deviceIds.Length > 0)
                {
                    entraDevicesUrl += $"?$filter=id%20eq%20%27{string.Join("'%20or%20id%20eq%20'", deviceIds)}%27";
                }
            }

            // Request devices from Entra
            Logger.Info("Requesting devices from Entra");
            HttpResponseMessage devicesResponse = await HttpHandler.GetAsync(entraDevicesUrl);
            string devicesResponseContent = await devicesResponse.Content.ReadAsStringAsync();

            if (devicesResponse.StatusCode == HttpStatusCode.NotFound)
            {
                return entraDevices;
            }
            else if (devicesResponse.StatusCode != HttpStatusCode.OK)
            {
                Logger.Error("Failed to get devices from Entra");
                JsonHandler.GetProperties(devicesResponseContent);
                return entraDevices;
            }

            // Deserialize the JSON response to a dictionary
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
                Logger.Info("No matching devices found");
                return entraDevices;
            }

            Logger.Info($"Found {devices.Count} matching {(devices.Count == 1 ? "device" : "devices")} in Entra");
            foreach (Dictionary<string, object> device in devices)
            {
                // Create an object for each item in the response
                var entraDevice = new EntraDevice(device, database);
                entraDevices.Add(entraDevice);
            }
            if (database != null)
            {
                Logger.Info($"Upserted {(devices.Count == 1 ? "device" : "devices")} in the database");
            }
            // Convert the devices ArrayList to JSON blob string
            string devicesJson = JsonConvert.SerializeObject(devices, Formatting.Indented);

            // Print the selected properties of the devices
            if (printJson) JsonHandler.GetProperties(devicesJson, false, properties);

            return entraDevices;
        }

        public async Task<List<JsonObject>> GetGroupMembers(string groupId, string searchForType, string[] properties = null, 
            bool printJson = false, LiteDBHandler database = null)
        {
            List<JsonObject> entraGroupMembers = new List<JsonObject>();

            string url = $"https://graph.microsoft.com/beta/groups/{groupId}/members?$select=id,deviceId,displayName,userType,appId,mail," +
                "onPremisesSyncEnabled,deviceId&$orderby=displayName%20asc&$count=true";
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Set required header per https://aka.ms/graph-docs/advanced-queries
            request.Headers.Add("Consistencylevel", "eventual");

            HttpResponseMessage groupMembersResponse = await HttpHandler.SendRequestAsync(request);
            string groupMembersResponseContent = await groupMembersResponse.Content.ReadAsStringAsync();

            if (groupMembersResponse.StatusCode != HttpStatusCode.OK)
            {
                Logger.Error("Failed to get group members from Entra");
                JsonHandler.GetProperties(groupMembersResponseContent);
                return null;
            }

            // Deserialize the JSON response to a dictionary
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            var fullResponseDict = serializer.Deserialize<Dictionary<string, object>>(groupMembersResponseContent);
            if (fullResponseDict == null) return null;

            var members = (ArrayList)fullResponseDict["value"];
            if (members.Count == 0)
            {
                Logger.Info($"No group members found in {groupId}");
                return null;
            }

            Logger.Info($"Found {members.Count} {(members.Count == 1 ? "member" : "members")} in {groupId}");
            foreach (Dictionary<string, object> member in members)
            {
                // Create an object for each item in the response
                if (member.TryGetValue("@odata.type", out var memberType))
                {
                    if (memberType.ToString().Contains("user"))
                    {
                        var entraUser = new EntraUser(member, database);
                        entraGroupMembers.Add(entraUser);
                    }
                    else if (memberType.ToString().Contains("group"))
                    {
                        var entraGroup = new EntraGroup(member, database);
                        entraGroupMembers.Add(entraGroup);
                    }
                    else if (memberType.ToString().Contains("device"))
                    {
                        var entraDevice = new EntraDevice(member, database);
                        entraGroupMembers.Add(entraDevice);
                    }
                    else
                    {
                        Logger.Warning("Unknown member type found in group members list");
                    }
                }
            }

            // Convert the groups ArrayList to JSON blob string
            string groupsJson = JsonConvert.SerializeObject(members, Formatting.Indented);

            // Print the selected properties of the devices
            if (printJson) JsonHandler.GetProperties(groupsJson, false, properties);

            if (searchForType != null)
            {
                entraGroupMembers = entraGroupMembers.Where(member => member.GetType().Name == searchForType).ToList();
            }
            return entraGroupMembers;
        }

        // get entra membership (group memberships for given object ID)
        public async Task<List<EntraGroup>> GetMembership(string specifiedObjectId = "", string specifiedObjectName = "", 
            string specifiedObjectType = "", string[] properties = null, LiteDBHandler database = null, bool printJson = true)
        {
            List<EntraGroup> memberOf = new List<EntraGroup>();

            Logger.Info($"Requesting group memberships for {(specifiedObjectName != null ? specifiedObjectName : specifiedObjectId)} from Entra");

            string objectId = "";
            string objectType = "";
            JsonObject entraObject = null;

            if (string.IsNullOrEmpty(specifiedObjectType))
            {
                Logger.Info("Checking if the object is a device, user, or group");
                (objectType, entraObject) = await GetObjectType(specifiedObjectId, specifiedObjectName, properties, database);
                if (objectType == null)
                {
                    Logger.Error("Failed to find the specified object");
                    return memberOf;
                }
                objectId = entraObject.Properties["id"].ToString();
            } 
            else
            {
                objectId = specifiedObjectId;
                objectType = specifiedObjectType;
            }

            string url = $"https://graph.microsoft.com/v1.0/{objectType}/{objectId}/memberOf/$/microsoft.graph.group?$select={string.Join(",",properties)}";

            HttpResponseMessage memberOfResponse = await HttpHandler.GetAsync(url);
            string memberOfResponseContent = await memberOfResponse.Content.ReadAsStringAsync();
            if (memberOfResponse.StatusCode != HttpStatusCode.OK)
            {
                Logger.Error("Failed to get group membership from Entra");
                JsonHandler.GetProperties(memberOfResponseContent);
                return memberOf;
            }

            // Deserialize the JSON response to a dictionary
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            var memberOfResponseDict = serializer.Deserialize<Dictionary<string, object>>(memberOfResponseContent);
            if (memberOfResponseDict == null) return memberOf;

            var groups = (ArrayList)memberOfResponseDict["value"];

            if (groups.Count > 0)
            {
                foreach (Dictionary<string, object> group in groups)
                {
                    // Create an object for each item in the response
                    var entraGroup = new EntraGroup(group, database);
                    memberOf.Add(entraGroup);
                    Logger.Info($"Found group membership for {(specifiedObjectName != null ? specifiedObjectName : specifiedObjectId)}: {entraGroup.Properties["displayName"]}");

                    // Recursive call to get nested group memberships
                    await GetMembership(entraGroup.Properties["id"].ToString(), entraGroup.Properties["displayName"].ToString(), "groups", properties, database, true);
                }

                // Combine all the group memberships into lists of IDs and displayNames
                List<string> memberOfGroupIds = memberOf.Select(group => group.Properties["id"].ToString()).ToList();
                List<string> memberOfGroupNames = memberOf.Select(group => group.Properties["displayName"].ToString()).ToList();

                // Set the memberOf properties for the specified object
                if (entraObject != null)
                {
                    entraObject.Properties.Add("memberOfGroupIds", memberOfGroupIds);
                    entraObject.Properties.Add("memberOfGroupNames", memberOfGroupNames);
                    entraObject.Upsert(database);
                }

                // Convert the ArrayList to JSON blob string
                string memberOfJson = JsonConvert.SerializeObject(groups, Formatting.Indented);

                // Print the selected properties
                if (printJson) JsonHandler.GetProperties(memberOfJson, false, properties);
            }
            else
            {
                Logger.Info($"No group membership found for {specifiedObjectName} ({specifiedObjectId})");
            }
            return memberOf;
        }


        public async Task<(string, JsonObject)> GetObjectType(string id = "", string name = "", string[] properties = null, 
            LiteDBHandler database = null)
        {
            EntraUser specifiedEntraUser = null;
            EntraDevice specifiedEntraDevice = null;
            EntraGroup specifiedEntraGroup = null;

            // See if the object is a device first
            specifiedEntraDevice = await GetDevice(id, id, name, properties, database);

            // Next, check users
            if (specifiedEntraDevice != null)
            {
                return ("devices", specifiedEntraDevice);
            }
            else
            {
                specifiedEntraUser = await GetUser(id, name, properties, database);
                if (specifiedEntraUser != null)
                {
                    return ("users", specifiedEntraUser);
                }
                else
                {
                    specifiedEntraGroup = await GetGroup(id, name, properties, database);
                    if (specifiedEntraGroup != null)
                    {
                        return ("groups", specifiedEntraGroup);
                    }
                    else
                    {
                        Logger.Error("Failed to find the specified object");
                        return (null, null);
                    }
                }
            }
        }

        public async Task<EntraUser> GetUser(string userId = "", string userName = "", string[] properties = null,
            LiteDBHandler database = null)
        {
            List<EntraUser> users = await GetUsers(userId, userName, properties, database, printJson: false);
            if (users.Count > 1)
            {
                Logger.Error("Multiple users found matching the specified name");
                return null;
            }
            else if (users.Count == 0)
            {
                Logger.Warning($"Failed to find the specified user");
                return null;
            }
            userId = users.FirstOrDefault()?.Properties["id"].ToString();
            return users.FirstOrDefault();
        }

        public async Task<List<EntraUser>> GetUsers(string userId = "", string userName = "", string[] properties = null, LiteDBHandler database = null, 
            bool printJson = true)
        {
            Logger.Info($"Requesting users from Entra");
            List<EntraUser> entraUsers = new List<EntraUser>();
            string url = $"https://graph.microsoft.com/beta/users";

            if (!string.IsNullOrEmpty(userId))
            {
                url += $"?filter=Id%20eq%20%27{userId}%27";
            }
            else if (!string.IsNullOrEmpty(userName))
            {
                url += $"?filter=userPrincipalName%20eq%20%27{userName}%27";
            }

            HttpResponseMessage usersResponse = await HttpHandler.GetAsync(url);
            string usersResponseContent = await usersResponse.Content.ReadAsStringAsync();

            if (usersResponse.StatusCode == HttpStatusCode.NotFound)
            {
                return entraUsers;
            }
            else if (usersResponse.StatusCode != HttpStatusCode.OK)
            {
                Logger.Error("Failed to get users from Entra");
                JsonHandler.GetProperties(usersResponseContent);
                return entraUsers;
            }

            // Deserialize the JSON response to a dictionary
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            var usersResponseDict = serializer.Deserialize<Dictionary<string, object>>(usersResponseContent);
            if (usersResponseDict == null) return null;

            var users = (ArrayList)usersResponseDict["value"];

            if (users.Count > 0)
            {
                Logger.Info($"Found {users.Count} {(users.Count == 1 ? "user" : "users")} in Entra");
                foreach (Dictionary<string, object> user in users)
                {
                    // Create an object for each item in the response
                    var entraUser = new EntraUser(user, database);
                    entraUsers.Add(entraUser);
                }
                // Convert the devices ArrayList to JSON blob string
                string usersJson = JsonConvert.SerializeObject(users, Formatting.Indented);

                // Print the selected properties of the devices
                if (printJson) JsonHandler.GetProperties(usersJson, false, properties);
            }
            else
            {
                Logger.Info("No users found");
            }
            return entraUsers;
        }

        public async Task<string> NewGroup(string groupName, string memberId = "")
        {
            Logger.Info("Adding new group to Entra");
            string url = "https://graph.microsoft.com/beta/groups";

            StringContent content = HttpHandler.CreateJsonContent(new
            {
                displayName = groupName,
                mailEnabled = false,
                mailNickname = groupName.Replace(" ", ""),
                securityEnabled = true,
            });

            HttpResponseMessage newGroupResponse = await HttpHandler.PostAsync(url, content);
            string newGroupResponseContent = await newGroupResponse.Content.ReadAsStringAsync();

            if (newGroupResponse.StatusCode != HttpStatusCode.Created)
            {
                Logger.Error("Failed to add new group to Entra");
                JsonHandler.GetProperties(newGroupResponseContent);
                return null;
            }

            // Parse the JSON response to get the new group ID
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            var newGroupResponseDict = serializer.Deserialize<Dictionary<string, object>>(newGroupResponseContent);
            if (newGroupResponseDict == null)
            {
                Logger.Error("Failed to parse new group ID from response");
                return null;
            }

            string newGroupId = newGroupResponseDict["id"].ToString();
            Logger.Info($"Successfully added new group to Entra: {newGroupId}");

            if (!string.IsNullOrEmpty(memberId))
            {
                HttpResponseMessage addMemberResponse = await AddGroupMember(newGroupId, memberId);
                if (addMemberResponse is null)
                {
                    Logger.Error("Failed to add member to new group");
                    return null;
                }
            }
            return newGroupId;
        }

        public List<EntraUser> ShowUsers(LiteDBHandler database, string[] properties, string userId = "")
        {
            List<EntraUser> entraUsers = new List<EntraUser>();

            if (!string.IsNullOrEmpty(userId))
            {
                var user = database.FindByPrimaryKey<EntraUser>(userId);
                if (user != null)
                {
                    Logger.Info($"Found a matching user in the database");
                    JsonHandler.GetProperties(user.ToString(), false, properties);
                    Dictionary<string, object> userProperties = BsonDocumentHandler.ToDictionary(user);
                    entraUsers.Add(new EntraUser(userProperties, database));
                }
                else 
                { 
                    Logger.Info("No matching user found in the database"); 
                }
            }
            else
            {
                var databaseUsers = database.FindInCollection<EntraUser>();
                if (databaseUsers.Any())
                {
                    Logger.Info($"Found {databaseUsers.Count()} matching users in the database");
                    foreach (var user in databaseUsers)
                    {
                        JsonHandler.GetProperties(user.ToString(), false, properties);
                        Dictionary<string, object> userProperties = BsonDocumentHandler.ToDictionary(user);
                        entraUsers.Add(new EntraUser(userProperties, database));
                    }
                }
                else
                {
                    Logger.Info("No matching users found in the database");
                }
            }
            return entraUsers;
        }
    }
}
