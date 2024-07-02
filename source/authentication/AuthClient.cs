using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Maestro
{
    public class AuthClient : IAuthClient
    {
        public IHttpHandler HttpHandler { get; }
        public string BearerToken { get; private set; }
        public string ImpersonationToken { get; private set; }
        public string RefreshToken { get; private set; }
        public string TenantId { get; private set; }

        public AuthClient()
        {
            HttpHandler = new HttpHandler();
        }

        public static async Task<AuthClient> InitAndGetAccessToken(string authRedirectUrl, string delegationTokenUrl, string extensionName, string resourceName, IDatabaseHandler database = null, string prtCookie = "", string bearerToken = "", bool reauth = false)
        {
            var client = new AuthClient();

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
                await client.Authenticate(authRedirectUrl, database, prtCookie);
                //client.BearerToken = await client.GetAccessToken(client.TenantId, client.RefreshToken,
                //    delegationTokenUrl, extensionName, resourceName, database);
                //client.HttpHandler.SetAuthorizationHeader(client.BearerToken);
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
                    HttpHandler.SetAuthorizationHeader(BearerToken);
                    return true;
                }
            }
            return false;
        }

        private async Task<HttpResponseMessage> AuthorizeWithPrtCookie(string url, string xMSRefreshtokencredential)
        {
            Logger.Info("Using PRT cookie to authenticate to authorize URL");

            // Set the primary refresh token header
            var prtCookie = new Cookie
            {
                Name = "X-Ms-Refreshtokencredential",
                Value = xMSRefreshtokencredential,
                Domain = "login.microsoftonline.com",
                Path = "/"
            };

            HttpHandler.CookiesContainer.Add(prtCookie);
            HttpResponseMessage authorizeResponse = await HttpHandler.GetAsync(url);

            if (!authorizeResponse.IsSuccessStatusCode)
            {
                Logger.Error("Failed to authenticate to authorize URL");
                return null;
            }

            Logger.Debug(await authorizeResponse.Content.ReadAsStringAsync());
            return authorizeResponse;
        }

        public async Task<string> GetAccessToken(string tenantId, string portalAuthorization, string delegationTokenUrl, string extensionName,
            string resourceName, IDatabaseHandler database)
        {
            Logger.Info("Requesting access token from DelegationToken endpoint with portalAuthorization");
            string accessToken = await GetAuthHeader(tenantId, portalAuthorization, delegationTokenUrl, extensionName, resourceName);
            if (accessToken is null)
            {
                Logger.Error("No authHeader was found in the DelegationToken response");
                return null;
            }
            Logger.Info($"Found access token in DelegationToken response: {accessToken}");

            // Store new JWT in the database
            if (database != null)
            {
                var jwt = new Jwt(accessToken);
                database.Upsert(jwt);
                Logger.Info("Upserted JWT in the database");
            }
            return accessToken;
        }

        private async Task<string> GetAuthHeader(string tenantId, string portalAuthorization, string delegationTokenUrl, string extensionName, 
            string resourceName)
        {
            var jsonObject = new
            {
                extensionName,
                resourceName,
                tenant = tenantId,
                portalAuthorization
            };
            var serializer = new JavaScriptSerializer();
            string json = serializer.Serialize(jsonObject);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await HttpHandler.PostAsync(delegationTokenUrl, content);
            string responseContent = await response.Content.ReadAsStringAsync();
            string authHeader = StringHandler.GetMatch(responseContent, "\"authHeader\":\"Bearer ([^\"]+)\"");
            return authHeader;
        }

        private async Task<string> GetAuthorizeUrl(string authRedirectUrl)
        {
            Logger.Info($"Requesting authorize URL from: {authRedirectUrl}");

            HttpResponseMessage idpRedirectResponse = await HttpHandler.GetAsync(authRedirectUrl);
            idpRedirectResponse.EnsureSuccessStatusCode();

            if (idpRedirectResponse is null)
            {
                Logger.Error("No response or empty response received for authorize URL request");
                return null;
            }
            string idpRedirectResponseContent = await idpRedirectResponse.Content.ReadAsStringAsync();
            string authorizeUrlPattern = @"https://login\.microsoftonline\.com/organizations/oauth2/v2\.0/authorize\?.*?(?=\"")";
            string authorizeUrl = StringHandler.GetMatch(idpRedirectResponseContent, authorizeUrlPattern, false);

            if (authorizeUrl is null) return null;
            Logger.Info("Found authorize URL");
            Logger.DebugTextOnly(authorizeUrl);
            return authorizeUrl;
        }

        public async Task<string> GetNonce()
        {
            string url = "https://login.microsoftonline.com/common/oauth2/token";
            Logger.Info($"Requesting nonce from {url}");

            var content = new StringContent("grant_type=srv_challenge", Encoding.UTF8, "application/x-www-form-urlencoded");
            HttpResponseMessage response = await HttpHandler.PostAsync(url, content);
            string responseContent = await response.Content.ReadAsStringAsync();
            return StringHandler.GetMatch(responseContent, "\"Nonce\":\"([^\"]+)\"");
        }

        public async Task<string> GetPrtCookie()
        {
            // Request nonce from token endpoint
            string ssoNonce = await GetNonce();
            if (ssoNonce is null)
            {
                Logger.Error("No nonce was found in the response");
                return null;
            }
            Logger.Info($"Found nonce in the response: {ssoNonce}");

            // Local request to mint PRT cookie with nonce
            return ROADToken.GetXMsRefreshtokencredential(ssoNonce);
        }

        public async Task<HttpResponseMessage> SignInToService(string redirectUrl, IDatabaseHandler database, string prtCookie = "")
        {

            // Get authorize endpoint from redirect
            string authorizeUrl = await GetAuthorizeUrl(redirectUrl);
            if (authorizeUrl is null)
                return null;

            if (string.IsNullOrEmpty(prtCookie))
            {
                // Get a nonce and primary refresh token cookie (x-Ms-Refreshtokencredential)
                prtCookie = await GetPrtCookie();
                if (prtCookie is null)
                    return null;
            }

            // HTTP request 3
            Logger.Info("Using PRT with nonce to obtain code+id_token required for signin");
            HttpResponseMessage authorizeWithSsoNonceResponse = await AuthorizeWithPrtCookie(authorizeUrl, prtCookie);
            if (authorizeWithSsoNonceResponse is null)
                return null;
            string authorizeWithSsoNonceResponseContent = await authorizeWithSsoNonceResponse.Content.ReadAsStringAsync();

            // Parse response for signin URL
            string actionUrl = StringHandler.GetMatch(authorizeWithSsoNonceResponseContent, 
                "<form method=\"POST\" name=\"hiddenform\" action=\"(.*?)\"");
            if (actionUrl is null)
            {
                Logger.Error("No hidden form action URLs were found in the response");
                return null;
            }
            Logger.Info($"Found hidden form action URL in the response: {actionUrl}");

            // Parse response for POST body with code+id_token
            FormUrlEncodedContent formData = ParseFormDataFromHtml(authorizeWithSsoNonceResponseContent);
            if (formData is null) 
                return null;

            Logger.Info("Signing in with code+id_token obtained from authorize endpoint");
            HttpResponseMessage signinResponse = await HttpHandler.PostAsync(actionUrl, formData);
            if (signinResponse is null)
            {
                Logger.Error("Could not sign in");
                return null;
            }
            Logger.Info("Obtained response from signin URL");
            return signinResponse;
        }


        // Submit refreshToken/PortalAuthorization blob to DelegationToken endpoint for PortalAuthorization blob
        private async Task<string> GetPortalAuthorization(string tenantId, string refreshToken, string url, string extensionName, string resourceName)
        {
            Logger.Info("Requesting portalAuthorization from DelegationToken endpoint with refreshToken");
            var jsonObject = new
            {
                extensionName,
                resourceName,
                tenant = tenantId,
                portalAuthorization = refreshToken
            };
            var serializer = new JavaScriptSerializer();
            string json = serializer.Serialize(jsonObject);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await HttpHandler.PostAsync(url, content);
            string responseContent = await response.Content.ReadAsStringAsync();
            string portalAuthorization = StringHandler.GetMatch(responseContent, "\\\"portalAuthorization\\\":\\\"([^\\\"]+)\\\"");
            if (portalAuthorization is null)
            {
                Logger.Error("No portalAuthorization was found in the DelegationToken response");
                return null;
            }
            Logger.Info($"Found portalAuthorization in DelegationToken response: {StringHandler.Truncate(portalAuthorization)}");
            Logger.Debug(portalAuthorization);
            return portalAuthorization;
        }

        public async Task Authenticate(string redirectUrl, IDatabaseHandler database = null, string prtCookie = "")
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            HttpResponseMessage signinResponse = await SignInToService(redirectUrl, database, prtCookie);
            if (signinResponse is null) return;

            string signinResponseContent = await signinResponse.Content.ReadAsStringAsync();
            string tenantId = ParseTenantIdFromJsonResponse(signinResponseContent);
            if (tenantId is null) return;
            TenantId = tenantId;

            // Get impersonation token for Azure portal
            OAuthToken oAuthToken = ParseOAuthTokenFromJsonResponse(signinResponseContent, database);
            if (oAuthToken is null) return;
            JObject oAuthTokenJ = (JObject)oAuthToken.GetProperties()["oAuthToken"];
            string authHeader = oAuthTokenJ.GetValue("authHeader").ToString();
            ImpersonationToken = StringHandler.GetMatch(authHeader, "Bearer ([^\"]+)");
            if (ImpersonationToken is null)
            {
                Logger.Error("No bearer token with the \"user_impersonation\" scope for Azure Portal was found in the response");
                return;
            }
            Logger.Info($"Found user_impersonation token for Azure Portal in response: {ImpersonationToken}");
            Logger.Debug(ImpersonationToken);

            // Get refresh token for future requests
            string refreshToken = ParseRefreshTokenFromJsonResponse(signinResponseContent);
            if (refreshToken is null) return;
            RefreshToken = refreshToken;
        }

        private FormUrlEncodedContent ParseFormDataFromHtml(string htmlContent)
        {
            FormUrlEncodedContent formData = null;

            // Get all hidden input fields
            MatchCollection inputs = Regex.Matches(
                htmlContent, "<input type=\"hidden\" name=\"(.*?)\" value=\"(.*?)\"");

            if (inputs.Count == 0)
            {
                Logger.Error("No hidden input fields were found in the response");
                return null;
            }
            Logger.Info("Found hidden input fields in the response");
            var formDataPairs = new Dictionary<string, string>();
            foreach (Match input in inputs)
            {
                formDataPairs[input.Groups[1].Value] = input.Groups[2].Value;
            }

            // Construct POST Request Data
            formData = new FormUrlEncodedContent(formDataPairs);
            return formData;
        }

        private string ParseInitialAuthorizeResponseForAuthorizeUrl(string authorizeResponse)
        {
            string pattern = "(?<=\"urlTenantedEndpointFormat\":\")(https:\\/\\/[^\",]+)";
            Match authorizeUrlWithSsoNonceMatch = Regex.Match(authorizeResponse, pattern);

            if (!authorizeUrlWithSsoNonceMatch.Success)
            {
                Logger.Error("No authorize URL was found in the response");
                return null;
            }

            // Replace Unicode in URL with corresponding characters and placeholder with "organizations"
            string ssoNonceUrl = Regex.Unescape(authorizeUrlWithSsoNonceMatch.Value).Replace(
                "{0}", "organizations");
            Logger.Info($"Found authorize URL in the response: {ssoNonceUrl}");
            return ssoNonceUrl;
        }

        private OAuthToken ParseOAuthTokenFromJsonResponse(string jsonResponse, IDatabaseHandler database = null)
        {
            // Parse response for OAuthToken
            string oAuthTokenBlob = StringHandler.GetMatch(jsonResponse, @"\{""oAuthToken"":\{.*?\}\}", false);
            if (oAuthTokenBlob is null)
            {
                Logger.Error("No oAuthToken was found in the response");
                return null;        
            }
            Logger.Info($"Found oAuthToken in response");
            Logger.Debug(oAuthTokenBlob);
            OAuthToken oAuthToken = new OAuthToken(oAuthTokenBlob);
            if (database != null)
            {
                database.Upsert(oAuthToken);
                Logger.Info("Upserted OAuthToken in the database");
            }
            return oAuthToken;
        }

        private string ParseRefreshTokenFromJsonResponse(string jsonResponse)
        {
            // Parse response for refreshToken
            string refreshToken = StringHandler.GetMatch(jsonResponse, "\\\"refreshToken\\\":\\\"([^\\\"]+)\\\"");
            if (refreshToken is null)
            {
                Logger.Error("No refreshToken was found in the response");
                return null;
            }
            Logger.Info($"Found refreshToken in response: {StringHandler.Truncate(refreshToken)}");
            Logger.Debug(refreshToken);
            return refreshToken;
        }

        private string ParseTenantIdFromJsonResponse(string jsonResponse)
        {
            // Parse response for tenantId
            string tenantId = StringHandler.GetMatch(jsonResponse, "\\\"tenantId\\\":\\\"([^\\\"]+)\\\"");
            if (tenantId is null)
            {
                Logger.Error("No tenantId was found in the response");
                return null;
            }
            Logger.Info($"Found tenantId in the response: {tenantId}");
            return tenantId;
        }
    }
}