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

        public async Task<HttpResponseMessage> GetGroups()
        {
            string url = "https://graph.microsoft.com/v1.0/$batch";
            var jsonObject = new
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
            };
            var serializer = new JavaScriptSerializer();
            string json = serializer.Serialize(jsonObject);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            return await _graphClient._httpHandler.PostAsync(url, content);
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
            List<EntraUser> entraUsers = new List<EntraUser>();

            Logger.Info($"Requesting users from Entra");
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
