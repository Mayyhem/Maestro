using System.Threading.Tasks;

namespace Maestro
{
    public class GraphClient
    {
        private readonly IAuthClient _authClient;
        public readonly IHttpHandler _httpHandler;
        public string BearerToken { get; protected set; }

        public GraphClient() 
        {
            _httpHandler = new HttpHandler();
            _authClient = new AuthClient(_httpHandler);
        }

        public static async Task<T> InitAndGetAccessToken<T>(string authRedirectUrl, string delegationTokenUrl, string extensionName, IDatabaseHandler database = null, string bearerToken = "", bool reauth = false)
             where T : GraphClient, new()
        {
            var client = new T();

            // Use the provided bearer token if available
            if (!string.IsNullOrEmpty(bearerToken))
            {
                client.BearerToken = bearerToken;
                if (database != null)
                {
                    Jwt accessToken = new Jwt(bearerToken);
                    database.Upsert(accessToken);
                    Logger.Info("Upserted JWT in the database");
                }
                return client;
            }

            // Check the database for a stored access token before fetching from Intune
            if (database != null && !reauth)
            {
                client.FindStoredAccessToken(database);
            }

            // Get a new access token if none found
            if (string.IsNullOrEmpty(client.BearerToken))
            {
                await client._authClient.Authenticate(authRedirectUrl, database);
                client.BearerToken = await client._authClient.GetAccessToken(client._authClient.TenantId, client._authClient.RefreshToken,
                    delegationTokenUrl, extensionName, "microsoft.graph", database);
                client._httpHandler.SetAuthorizationHeader(client.BearerToken);
            }
            return client;
        }

        public bool FindStoredAccessToken(IDatabaseHandler database, string scope = "")
        {
            if (!string.IsNullOrEmpty(BearerToken))
            {
                Logger.Info("Using bearer token from prior request");
                return true;
            }
            if (database != null)
            {
                BearerToken = database.FindValidJwt(scope);

                if (!string.IsNullOrEmpty(BearerToken))
                {
                    _httpHandler.SetAuthorizationHeader(BearerToken);
                    return true;
                }
            }
            return false;
        }
    }
}
