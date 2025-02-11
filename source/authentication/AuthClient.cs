using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Maestro
{
    public class AuthClient
    {
        public HttpHandler HttpHandler { get; }
        public string BearerToken { get; private set; }
        public string ClientId { get; private set; }
        public string Nonce { get; private set; }
        public string PortalAuthorization { get; private set; }
        public string PrtCookie { get; private set; }
        public string RefreshToken { get; private set; }
        public string Resource { get; private set; }
        public string Scope { get; private set; }
        public string SessionId { get; private set; }
        public string SpaAuthCode { get; private set; }
        public string TenantId { get; private set; }

        public AuthClient(string userAgent, string proxyUrl = "")
        {
            HttpHandler = new HttpHandler(userAgent, proxyUrl);
        }

        public AuthClient(string userAgent, string clientId, string resource, string scope, string refreshToken = "", string tenantId = "", string proxyUrl = "")
        {
            HttpHandler = new HttpHandler(userAgent, proxyUrl);
            ClientId = clientId;
            Resource = resource;
            Scope = scope;
            RefreshToken = refreshToken;
            TenantId = tenantId;
        }

        public static async Task<AuthClient> InitAndGetAccessToken(CommandLineOptions options, LiteDBHandler database, string idpRedirectUrl = "", string delegationTokenUrl = "")
        {
            return await InitAndGetAccessToken(idpRedirectUrl, delegationTokenUrl, options.Extension, options.Resource, database, 
                options.PrtCookie, options.RefreshToken, options.AccessToken, options.Reauth, options.Scope, options.PrtMethod, 
                options.TokenMethod, options.ClientId, options.TenantId, options.UserAgent, options.Proxy, options.Redirect, 
                options.Broker, options.BrkClientId, options);
        }
        public static async Task<AuthClient> InitAndGetAccessToken(string idpRedirectUrl, 
            string delegationTokenUrl, string extensionName, string resource, LiteDBHandler database = null,
            string providedPrtCookie = "", string providedRefreshToken = "", string providedAccessToken = "", 
            bool reauth = false, string scope = "", int prtMethod = 0, int accessTokenMethod = 0, 
            string clientId = "", string tenantId = "", string userAgent = "", string proxyUrl = "", string redirectUri = "", 
            bool broker = false, string brokerClientId = "", CommandLineOptions options = null)
        {
            var client = new AuthClient(userAgent, clientId, resource, scope, providedRefreshToken, tenantId, proxyUrl);
            AccessToken accessToken = null;

            // Use the provided access token if available
            if (!string.IsNullOrEmpty(providedAccessToken))
            {
                Logger.Info("Using provided access token");
                client.BearerToken = providedAccessToken;
                _ = new AccessToken(providedAccessToken, database);
                client.HttpHandler.SetAuthorizationHeader(client.BearerToken);
                // Test request to see if the token is valid
                HttpResponseMessage response = await client.HttpHandler.GetAsync("https://graph.microsoft.com/v1.0/me");
                if (!response.IsSuccessStatusCode)
                {
                    Logger.Error("Provided access token is invalid");
                    Logger.ErrorJson(await response.Content.ReadAsStringAsync());
                    return null;
                }
                return client;
            }

            // Use the provided refresh token if available
            else if (!string.IsNullOrEmpty(providedRefreshToken))
            {
                client.RefreshToken = providedRefreshToken;
                _ = new RefreshToken(providedRefreshToken, "", client.TenantId, client.ClientId, null, database);
            }

            // Use the provided PRT cookie if available
            else if (!string.IsNullOrEmpty(providedPrtCookie))
            {
                client.PrtCookie = providedPrtCookie;
                _ = new PrtCookie(providedPrtCookie, database);
            }

            // Check the database for a stored tokens before fetching from Intune
            if (database != null && !reauth)
            {
                bool foundAccessToken = client.FindStoredAccessToken(database, client.Scope);
                if (!foundAccessToken)
                {
                    bool foundRefreshToken = client.FindStoredRefreshToken(database, client.ClientId);
                    if (!foundRefreshToken)
                    {
                        client.FindStoredPrtCookie(database);
                    }
                }
            }

            // Get a new access token if none is found in the database
            if (string.IsNullOrEmpty(client.BearerToken))
            {
                if (accessTokenMethod == 2)
                {
                    // Get access token using MSAL
                    accessToken = await SharpGetEntraToken.Execute(client.HttpHandler._httpClient, clientId, tenantId, scope, database);
                    if (accessToken is null)
                        return null;
                }
                else
                {
                    if (string.IsNullOrEmpty(client.RefreshToken))
                    {
                        bool success = await client.AuthenticateWithPrt(idpRedirectUrl, redirectUri, database, prtMethod);
                        if (!success)
                        {
                            Logger.Error("Failed to authenticate");
                            return null;
                        }
                    }

                    if (accessTokenMethod == 0)
                    {
                        if (string.IsNullOrEmpty(client.RefreshToken))
                        {
                            /*
                            Logger.Info("Getting user_impersonation token for Azure Portal to management.core.windows.net (requires spaAuthCode)");
                            accessToken = await client.AuthToTokenEndpoint(options, database, "c44b4083-3bb0-49c1-b47d-974e53cbdf3c",
                                null, scope, client.TenantId, client.SpaAuthCode);
                            if (accessToken is null)
                                return null;

                            Logger.Info("Getting access token for Azure Portal to Azure Portal (requires refreshToken)");
                            accessToken = await client.AuthToTokenEndpoint(options, database, "c44b4083-3bb0-49c1-b47d-974e53cbdf3c",
                                "c44b4083-3bb0-49c1-b47d-974e53cbdf3c", scope, client.TenantId,
                                client.SpaAuthCode);
                            if (accessToken is null)
                                return null;

                            */
                            Logger.Info("Getting user_impersonation token for Azure Portal");
                            accessToken = await client.AuthToTokenEndpoint(options, database, "c44b4083-3bb0-49c1-b47d-974e53cbdf3c",
                                client.ClientId, scope, client.TenantId,
                                client.SpaAuthCode);
                            if (accessToken is null)
                                return null;
                        }

                        // Get scoped access and refresh tokens from /oauth/v2.0/token endpoint (requires refreshToken)
                        accessToken = await client.AuthToTokenEndpoint(options, database, client.ClientId,
                            client.Resource, client.Scope, client.TenantId, null, client.RefreshToken, broker,
                            brokerClientId, redirectUri);
                        if (accessToken is null)
                            return null;
                    }
                    else if (accessTokenMethod == 1)
                    {
                        resource = "microsoft.graph";
                        // Get access token from /api/DelegationToken endpoint (requires portalAuthorization)
                        accessToken = await client.GetAccessTokenFromDelegationTokenEndpoint(client.TenantId,
                            client.PortalAuthorization, delegationTokenUrl, extensionName, resource, database);
                        if (accessToken is null)
                            return null;
                    }
                }
                client.BearerToken = accessToken.Value;
                client.HttpHandler.SetAuthorizationHeader(client.BearerToken);
            }
            return client;
        }

        public bool FindStoredAccessToken(LiteDBHandler database, string scope = "")
        {
            if (!string.IsNullOrEmpty(BearerToken))
            {
                Logger.Info($"Using bearer token from prior request with scope: {scope}");
                return true;
            }
            if (database != null)
            {
                BearerToken = database.FindValidAccessToken(scope);

                if (!string.IsNullOrEmpty(BearerToken))
                {
                    HttpHandler.SetAuthorizationHeader(BearerToken);
                    return true;
                }
            }
            return false;
        }

        public bool FindStoredRefreshToken(LiteDBHandler database, string clientId)
        {
            if (!string.IsNullOrEmpty(RefreshToken))
            {
                Logger.Info($"Using refresh token from prior request with client ID: {clientId}");
                return true;
            }
            if (database != null)
            {
                RefreshToken = database.FindValidRefreshToken(clientId);

                if (!string.IsNullOrEmpty(RefreshToken))
                {
                    return true;
                }
            }
            return false;
        }

        public bool FindStoredPrtCookie(LiteDBHandler database)
        {
            if (!string.IsNullOrEmpty(PrtCookie))
            {
                Logger.Info("Using PRT cookie from prior request");
                return true;
            }
            if (database != null)
            {
                PrtCookie = database.FindValidPrtCookie();
                if (!string.IsNullOrEmpty(PrtCookie))
                {
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
            HttpResponseMessage authorizeResponse = await HttpHandler.GetAsync(url, isJsonResponse: false);
            return authorizeResponse;
        }

        public async Task<AccessToken> GetAccessTokenFromDelegationTokenEndpoint(string tenantId, string portalAuthorization, string delegationTokenUrl, string extensionName,
            string resourceName, LiteDBHandler database)
        {
            Logger.Info("Requesting access token from DelegationToken endpoint with portalAuthorization");
            string authHeader = await GetAuthHeader(tenantId, portalAuthorization, delegationTokenUrl, extensionName, resourceName);
            if (authHeader is null)
            {
                Logger.Error("No authHeader was found in the DelegationToken response");
                return null;
            }
            Logger.Info($"Found access token in DelegationToken response: {authHeader}");
            AccessToken accessToken = new AccessToken(authHeader, database);
            return accessToken;
        }

        public async Task<AccessToken> AuthToTokenEndpoint(CommandLineOptions options, LiteDBHandler database, 
            string clientId = "", string resource = "", string scope = "", string tenantId = "", 
            string spaAuthCode = "", string refreshToken = "", bool broker = false, string brkClientId = "", string redirectUri = "")
        {
            string url = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";
            OAuthTokenResponse tokenResponse = null;

            if (!string.IsNullOrEmpty(spaAuthCode) || !string.IsNullOrEmpty(refreshToken))
            {
                Logger.Info($"Requesting access token from /oauth2/v2.0/token endpoint with {(string.IsNullOrEmpty(spaAuthCode) ? "refreshToken" : "spaAuthCode")} for scope: {scope}");

                if (!string.IsNullOrEmpty(resource))
                {
                    resource += "/";
                }

                var parameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("scope", string.IsNullOrEmpty(spaAuthCode) ? scope : resource + scope),
                    new KeyValuePair<string, string>(string.IsNullOrEmpty(spaAuthCode) ? "refresh_token" : "code", string.IsNullOrEmpty(spaAuthCode) ? refreshToken : spaAuthCode),
                    new KeyValuePair<string, string>("grant_type", string.IsNullOrEmpty(spaAuthCode) ? "refresh_token" : "authorization_code")
                };

                if (broker)
                {
                    parameters.Add(new KeyValuePair<string, string>("brk_client_id", "c44b4083-3bb0-49c1-b47d-974e53cbdf3c"));
                    parameters.Add(new KeyValuePair<string, string>("redirect_uri", $"brk-c44b4083-3bb0-49c1-b47d-974e53cbdf3c://{options.Target}"));
                }

                else if (!string.IsNullOrEmpty(brkClientId) && !string.IsNullOrEmpty(redirectUri))
                {
                    parameters.Add(new KeyValuePair<string, string>("brk_client_id", brkClientId));
                    parameters.Add(new KeyValuePair<string, string>("redirect_uri", redirectUri));
                }

                var content = new FormUrlEncodedContent(parameters);

                // AADSTS9002327: Tokens issued for the 'Single-Page Application' client-type may only be redeemed via cross-origin requests. 
                HttpHandler.SetHeader("Origin", "https://portal.azure.com");
                HttpResponseMessage response = await HttpHandler.PostAsync(url, content);
                string responseContent = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    // Create objects and store tokens in database if specified
                    tokenResponse = new OAuthTokenResponse(responseContent, database);
                    if (tokenResponse is null)
                    {
                        Logger.Error("No tokens were found in the /oauth2/v2.0/token response");
                        Logger.ErrorJson(responseContent, options.Raw);
                        return null;
                    }

                    // Set AuthClient properties
                    BearerToken = tokenResponse.AccessToken.Value;
                    if (string.IsNullOrEmpty(BearerToken))
                    {
                        Logger.Error("No access token was found in the /oauth2/v2.0/token response");
                        Logger.ErrorJson(responseContent, options.Raw);
                        return null;
                    }

                    RefreshToken = tokenResponse.RefreshToken.Value;
                    if (string.IsNullOrEmpty(RefreshToken))
                    {
                        Logger.Error("No refresh token was found in the /oauth2/v2.0/token response");
                        Logger.ErrorJson(responseContent, options.Raw);
                        return null;
                    }
                }
                else
                {
                    Logger.Error("Unable to get tokens from /oauth2/v2.0/token endpoint");
                    Logger.ErrorJson(responseContent, options.Raw);
                    return null;
                }

                Logger.Info($"Found access token in /oauth2/v2.0/token response");
                Logger.Info($"Access token: {BearerToken}");
                Logger.Info($"Found refresh token in /oauth2/v2.0/token response");
                Logger.Info($"Refresh token: {RefreshToken}");

                return tokenResponse.AccessToken;
            }
            else
            {
                Logger.Error("Neither spaAuthCode nor refreshToken was provided");
                return null;
            }
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

        private async Task<string> GetAuthorizeRedirectUri(string idpRedirectUrl)
        {
            Logger.Info($"Requesting authorize URL from: {idpRedirectUrl}");

            HttpResponseMessage idpRedirectResponse = await HttpHandler.GetAsync(idpRedirectUrl, isJsonResponse: false);
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
            Logger.Verbose("Found authorize URL");
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

        public async Task<string> GetPrtCookie(int method, LiteDBHandler database)
        {
            // Request nonce from token endpoint
            string ssoNonce = await GetNonce();
            if (ssoNonce is null)
            {
                Logger.Error("No nonce was found in the response");
                return null;
            }
            Logger.Verbose($"Found nonce in the response: {ssoNonce}");
            Nonce = ssoNonce;

            // Local request to mint PRT cookie with nonce
            PrtCookie prtCookie = null;
            if (method == 0)
            {
                Logger.Info("Requesting PRT cookie with nonce for this user/device from LSA/CloudAP via COM");
                List<PrtCookie> prtCookies = RequestAADRefreshToken.GetPrtCookies(database, ssoNonce);
                if (prtCookies.Count > 0)
                {
                    prtCookie = prtCookies.First();
                }
                else
                {
                    Logger.Error("No x-ms-RefreshTokenCredential PRT cookies were found for this user");
                    return null;
                }
            }
            else if (method == 1)
            {
                prtCookie = ROADToken.GetPrtCookie(database, ssoNonce);
            }
            return prtCookie.Value;
        }

        public async Task<HttpResponseMessage> SignInWithPrt(string idpRedirectUrl, LiteDBHandler database = null, int prtMethod = 0)
        {
            // Get authorize redirect URI from idpRedirect.js
            string authorizeUrl = await GetAuthorizeRedirectUri(idpRedirectUrl);
            if (authorizeUrl is null)
                return null;

            // Parse authorize URL for client_id and scope
            if (prtMethod == 1)
            {
                ClientId = StringHandler.GetMatch(authorizeUrl, "client_id=(.*?)&");
                Scope = StringHandler.GetMatch(authorizeUrl, "scope=(.*?)&");
            }
            
            if (string.IsNullOrEmpty(PrtCookie))
            {
                // Get a nonce and primary refresh token cookie (x-Ms-Refreshtokencredential)
                PrtCookie = await GetPrtCookie(prtMethod, database);
                if (PrtCookie is null)
                    return null;
            }

            // Use PRT cookie to obtain code+id_token required for signin
            Logger.Info("Requesting code+id_token required for signin using PRT cookie with nonce");
            HttpResponseMessage authorizeResponse = await AuthorizeWithPrtCookie(authorizeUrl, PrtCookie);
            if (authorizeResponse is null)
                return null;

            string authorizeWithSsoNonceResponseContent =
                await authorizeResponse.Content.ReadAsStringAsync();

            // Not the best way to check this but it works for now
            if (authorizeWithSsoNonceResponseContent.Contains("MFA"))
            {
                Logger.Error("MFA may be required and the PRT did not contain the required claim");
                return null;
            }
        
            // Parse response for hidden form action URL
            string actionUrl = StringHandler.GetMatch(authorizeWithSsoNonceResponseContent,
                "<form method=\"POST\" name=\"hiddenform\" action=\"(.*?)\"");
            if (actionUrl is null)
            {
                Logger.Error("No hidden form action URL was found in the response");
                return null;
            }
            Logger.Verbose($"Found hidden form action URL in the response: {actionUrl}");

            // Parse response for POST body with code+id_token
            FormUrlEncodedContent formData = ParseFormDataFromHtml(authorizeWithSsoNonceResponseContent);
            if (formData is null)
                return null;

            string decodedFormData = await formData.ReadAsStringAsync();
            if (!decodedFormData.Contains("code") || !decodedFormData.Contains("id_token"))
            {
                Logger.Error("No code+id_token were found in the response");
                return null;
            }
            
            Logger.Info("Signing in with code+id_token obtained from authorize endpoint");
            HttpResponseMessage signinResponse = await HttpHandler.PostAsync(actionUrl, formData);
            if (signinResponse is null)
            {
                Logger.Error("Could not sign in");
                return null;
            }
            Logger.Verbose("Obtained response from signin URL");
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
            Logger.Verbose($"Found portalAuthorization in DelegationToken response: {StringHandler.Truncate(portalAuthorization, 12)}");
            Logger.DebugTextOnly(portalAuthorization);
            return portalAuthorization;
        }

        public async Task<bool> AuthenticateWithPrt(string idpRedirectUrl, string authorizeRedirectUri = "", LiteDBHandler database = null, int prtMethod = 0)
        {
            HttpResponseMessage signinResponse = await SignInWithPrt(idpRedirectUrl, database, prtMethod);
            if (signinResponse is null) return false;

            string signinResponseContent = await signinResponse.Content.ReadAsStringAsync();

            // Get tenantId for subsequent requests
            string tenantId = ParseTenantIdFromJsonResponse(signinResponseContent);

            // Get spaAuthCode and portalAuthorization for subsequent requests
            string spaAuthCode = ParseSpaAuthCodeFromJsonResponse(signinResponseContent, database);
            string portalAuthorization = ParsePortalAuthorizationFromJsonResponse(signinResponseContent);

            if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(spaAuthCode) || string.IsNullOrEmpty(portalAuthorization))
            {
                Logger.Error("Signin failed, possibly due to MFA requirements");
                Logger.DebugTextOnly(signinResponseContent);
                return false;
            }

            return true;
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
            Logger.Verbose("Found hidden input fields in the response");
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
            Logger.Verbose($"Found authorize URL in the response: {ssoNonceUrl}");
            return ssoNonceUrl;
        }

        private OAuthTokenDynamic ParseOAuthTokenFromJsonResponse(string jsonResponse, LiteDBHandler database = null)
        {
            // Parse response for OAuthToken
            string oAuthTokenBlob = StringHandler.GetMatch(jsonResponse, @"\{""oAuthToken"":\{.*?\}\}", false);
            if (oAuthTokenBlob is null)
            {
                Logger.Error("No oAuthToken was found in the response");
                return null;        
            }
            Logger.Verbose($"Found oAuthToken in response");
            Logger.DebugTextOnly(oAuthTokenBlob);
            OAuthTokenDynamic oAuthToken = new OAuthTokenDynamic(oAuthTokenBlob, database);
            return oAuthToken;
        }

        private string ParsePortalAuthorizationFromJsonResponse(string jsonResponse)
        {
            // Parse response for refreshToken
            string portalAuthorization = StringHandler.GetMatch(jsonResponse, "\\\"refreshToken\\\":\\\"([^\\\"]+)\\\"");
            if (portalAuthorization is null)
            {
                Logger.Error("No portal authorization (refreshToken) was found in the response");
                return null;
            }
            Logger.Verbose($"Found portal authorization (refreshToken) in response: {StringHandler.Truncate(portalAuthorization, 12)}");
            PortalAuthorization = portalAuthorization;
            Logger.DebugTextOnly(portalAuthorization);
            return portalAuthorization;
        }

        private string ParseSpaAuthCodeFromJsonResponse(string jsonResponse, LiteDBHandler database = null)
        {
            // Parse response for spaAuthCode
            string spaAuthTokenBlob = StringHandler.GetMatch(jsonResponse, @"""spaAuthCode"":""(.*?)""", true);
            if (spaAuthTokenBlob is null)
            {
                Logger.Error("No spaAuthCode was found in the response");
                return null;
            }
            Logger.Verbose($"Found spaAuthCode in response: {StringHandler.Truncate(spaAuthTokenBlob, 12)}");
            SpaAuthCode = spaAuthTokenBlob;
            Logger.DebugTextOnly(spaAuthTokenBlob);
            return spaAuthTokenBlob;
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
            Logger.Verbose($"Found tenantId in the response: {tenantId}");
            TenantId = tenantId;
            return tenantId;
        }
    }
}