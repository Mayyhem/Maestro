using Newtonsoft.Json;
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
    // EntraID Microsoft Graph client
    public class EntraClient
    {
        private GraphClient _graphClient;
        public EntraClient() 
        {
            _graphClient = new GraphClient();
        }

        public static async Task<EntraClient> InitAndGetAccessToken(IDatabaseHandler database, string bearerToken = "", bool reauth = false)
        {
            var entraClient = new EntraClient();
            string authRedirectUrl = "https://portal.azure.com/signin/idpRedirect.js";
            string delegationTokenUrl = "https://portal.azure.com/api/DelegationToken";
            string extensionName = "Microsoft_AAD_IAM";
            entraClient._graphClient = await GraphClient.InitAndGetAccessToken<GraphClient>(authRedirectUrl, delegationTokenUrl, extensionName, 
                database, bearerToken, reauth);
            return entraClient;
        }


        // entra groups
        public async Task<List<EntraGroup>> GetGroups(string groupId = "", string[] properties = null, IDatabaseHandler database = null,
            bool printJson = true)
        {
            Logger.Info($"Requesting groups from Entra");
            List<EntraGroup> entraGroups = new List<EntraGroup>();
            string url = "https://graph.microsoft.com/v1.0/$batch";
            var content = _graphClient._httpHandler.CreateJsonContent(new
            {
                requests = new[]
                {
                    new
                    {
                        id = "SecurityEnabledGroups",
                        method = "GET",
                        url = "groups?$select=displayName,mail,id,onPremisesSyncEnabled,onPremisesLastSyncDateTime,groupTypes," +
                        "mailEnabled,securityEnabled,resourceProvisioningOptions,isAssignableToRole&$top=100&$filter=securityEnabled eq true",
                        headers = new Dictionary<string, object>()
                    }
                }
            });
    
            HttpResponseMessage groupsResponse = await _graphClient._httpHandler.PostAsync(url, content);
            string groupsResponseContent = await groupsResponse.Content.ReadAsStringAsync();

            if (groupsResponse.StatusCode != HttpStatusCode.OK)
            {
                Logger.Error("Failed to get groups from Entra");
                JsonHandler.PrintProperties(groupsResponseContent);
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
                var entraGroup = new EntraGroup(group);
                entraGroups.Add(entraGroup);
                if (database != null)
                {
                    // Insert new record if matching id doesn't exist
                    database.Upsert(entraGroup);
                }
            }
            if (database != null)
            {
                Logger.Info("Upserted groups in the database");
            }

            // Convert the groups ArrayList to JSON blob string
            string groupsJson = JsonConvert.SerializeObject(groups, Formatting.Indented);

            // Print the selected properties of the devices
            if (printJson) JsonHandler.PrintProperties(groupsJson, properties);
            return entraGroups;
        }

        public List<EntraGroup> ShowGroups(IDatabaseHandler database, string[] properties, string groupId = "")
        {
            List<EntraGroup> entraGroups = new List<EntraGroup>();

            if (!string.IsNullOrEmpty(groupId))
            {
                var group = database.FindByPrimaryKey<EntraGroup>(groupId);
                if (group != null)
                {
                    Logger.Info($"Found a matching group in the database");
                    JsonHandler.PrintProperties(group.ToString(), properties);
                    Dictionary<string, object> groupProperties = BsonDocumentHandler.ToDictionary(group);
                    entraGroups.Add(new EntraGroup(groupProperties));
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
                        JsonHandler.PrintProperties(group.ToString(), properties);
                        Dictionary<string, object> groupProperties = BsonDocumentHandler.ToDictionary(group);
                        entraGroups.Add(new EntraGroup(groupProperties));
                    }
                }
                else
                {
                    Logger.Info("No matching groups found in the database");
                }
            }
            return entraGroups;
        }

        public async Task<HttpResponseMessage> GetGroupMembers(string groupId)
        {
            string url = $"https://graph.microsoft.com/beta/groups/{groupId}/members?$select=id,displayName,userType,appId,mail," +
                "onPremisesSyncEnabled,deviceId&$orderby=displayName%20asc&$count=true";
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Set required header per https://aka.ms/graph-docs/advanced-queries
            request.Headers.Add("Consistencylevel", "eventual");

            return await _graphClient._httpHandler.SendRequestAsync(request);
        }

        public async Task<EntraUser> GetUser(string userId = "", string userName = "", string[] properties = null,
            IDatabaseHandler database = null)
        {
            List<EntraUser> users = await GetUsers(userId, properties, database, printJson: false);
            if (users.Count > 1)
            {
                Logger.Error("Multiple users found matching the specified name");
                return null;
            }
            else if (users.Count == 0)
            {
                Logger.Error($"Failed to find the specified user");
                return null;
            }
            userId = users.FirstOrDefault()?.Properties["id"].ToString();
            return users.FirstOrDefault();
        }

        public async Task<List<EntraUser>> GetUsers(string userId = "", string[] properties = null, IDatabaseHandler database = null, 
            bool printJson = true)
        {
            Logger.Info($"Requesting users from Entra");
            List<EntraUser> entraUsers = new List<EntraUser>();
            string url = $"https://graph.microsoft.com/beta/users";

            if (!string.IsNullOrEmpty(userId))
            {
                url += $"?filter=Id%20eq%20%27{userId}%27";
            }
            HttpResponseMessage usersResponse = await _graphClient._httpHandler.GetAsync(url);
            string usersResponseContent = await usersResponse.Content.ReadAsStringAsync();
            if (usersResponse.StatusCode != HttpStatusCode.OK)
            {
                Logger.Error("Failed to get users from Entra");
                JsonHandler.PrintProperties(usersResponseContent);
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
                    var entraUser = new EntraUser(user);
                    entraUsers.Add(entraUser);
                    if (database != null)
                    {
                        // Insert new record if matching id doesn't exist
                        database.Upsert(entraUser);
                    }
                }
                if (database != null)
                {
                    Logger.Info("Upserted users in the database");
                }
                // Convert the devices ArrayList to JSON blob string
                string usersJson = JsonConvert.SerializeObject(users, Formatting.Indented);

                // Print the selected properties of the devices
                if (printJson) JsonHandler.PrintProperties(usersJson, properties);
            }
            else
            {
                Logger.Info("No users found");
            }
            return entraUsers;
        }

        public List<EntraUser> ShowUsers(IDatabaseHandler database, string[] properties, string userId = "")
        {
            List<EntraUser> entraUsers = new List<EntraUser>();

            if (!string.IsNullOrEmpty(userId))
            {
                var user = database.FindByPrimaryKey<EntraUser>(userId);
                if (user != null)
                {
                    Logger.Info($"Found a matching user in the database");
                    JsonHandler.PrintProperties(user.ToString(), properties);
                    Dictionary<string, object> userProperties = BsonDocumentHandler.ToDictionary(user);
                    entraUsers.Add(new EntraUser(userProperties));
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
                        JsonHandler.PrintProperties(user.ToString(), properties);
                        Dictionary<string, object> userProperties = BsonDocumentHandler.ToDictionary(user);
                        entraUsers.Add(new EntraUser(userProperties));
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
