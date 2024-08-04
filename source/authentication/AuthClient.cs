using Newtonsoft.Json.Linq;
using System;
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
        public string PortalAuthorization { get; private set; }
        public string PrtCookie { get; private set; }
        public string RefreshToken { get; private set; }
        public string Resource { get; private set; }
        public string Scope { get; private set; }
        public string SessionId { get; private set; }
        public string SpaAuthCode { get; private set; }
        public string TenantId { get; private set; }

        public AuthClient()
        {
            HttpHandler = new HttpHandler();
        }

        public static async Task<AuthClient> InitAndGetAccessToken(string authRedirectUrl, 
            string delegationTokenUrl, string extensionName, string resourceName, LiteDBHandler database = null,
            string providedPrtCookie = "", string providedRefreshToken = "", string providedAccessToken = "", 
            bool reauth = false, string requiredScope = "", int prtMethod = 0, int accessTokenMethod = 0)
        {
            var client = new AuthClient();
            AccessToken accessToken = null;

            // Use the provided access token if available
            if (!string.IsNullOrEmpty(providedAccessToken))
            {
                client.BearerToken = providedAccessToken;
                accessToken = new AccessToken(providedAccessToken, database);
                return client;
            }

            // Check the database for a stored access token before fetching from Intune
            if (database != null && !reauth)
            {
                client.FindStoredAccessToken(database, requiredScope);
            }

            // Get a new access token if none is found in the database
            if (string.IsNullOrEmpty(client.BearerToken))
            {
                // Generate a new GUID for the session ID
                Guid newGuid = Guid.NewGuid();

                // Format the GUID as a 32-character lowercase string without hyphens
                string sessionId = newGuid.ToString("N");
                client.SessionId = sessionId;

                string authRedirectUrlWithSessionId = $"{authRedirectUrl}/?sessionId={sessionId}";
                await client.Authenticate(authRedirectUrlWithSessionId, database, providedPrtCookie, prtMethod);

                if (accessTokenMethod == 0)
                {
                    // Get access token from oauth/v2.0/token endpoint (requires spaAuthCode)
                    accessToken = await client.GetAccessTokenFromTokenEndpoint(database, client.ClientId,
                        client.Scope, client.SpaAuthCode, client.RefreshToken, client.TenantId);
                }
                else if (accessTokenMethod == 1)
                {
                    // Get access token from /api/DelegationToken endpoint (requires portalAuthorization)
                    accessToken = await client.GetAccessTokenFromDelegationTokenEndpoint(client.TenantId,
                        client.PortalAuthorization, delegationTokenUrl, extensionName, resourceName, database);
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
                Logger.Info("Using bearer token from prior request");
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

        public async Task<AccessToken> GetAccessTokenFromTokenEndpoint(LiteDBHandler database, string clientId = "", string scope = "", string spaAuthCode = "", string refreshToken = "", string tenantId = "")
        {
            if (!string.IsNullOrEmpty(spaAuthCode))
            {
                Logger.Info("Requesting access token from /oauth2/v2.0/token endpoint with spaAuthCode");
                string url = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("scope", scope),
                    new KeyValuePair<string, string>("code", spaAuthCode),
                    new KeyValuePair<string, string>("grant_type", "authorization_code")
                });

                // AADSTS9002327: Tokens issued for the 'Single-Page Application' client-type may only be redeemed via cross-origin requests. 
                HttpHandler.SetHeader("Origin", "https://portal.azure.com");
                HttpResponseMessage response = await HttpHandler.PostAsync(url, content);

                OAuthTokenResponse tokenResponse = null;
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    tokenResponse = new OAuthTokenResponse(responseContent, database);

                    BearerToken = tokenResponse.AccessToken.Value;
                    if (string.IsNullOrEmpty(BearerToken))
                    {
                        Logger.Error("No access token was found in the /oauth2/v2.0/token response");
                    }

                    RefreshToken = tokenResponse.RefreshToken.Value;
                    if (string.IsNullOrEmpty(RefreshToken))
                    {
                        Logger.Error("No refresh token was found in the /oauth2/v2.0/token response");
                    }
                }

                Logger.Info($"Found tokens in /oauth2/v2.0/token response");
                Logger.Info($"Access token: {BearerToken}");
                Logger.Info($"Refresh token: {RefreshToken}");
                return tokenResponse.AccessToken;
            }

            else if (!string.IsNullOrEmpty(refreshToken))
            {
                Logger.Info("Requesting access token from /oauth2/v2.0/token endpoint with refreshToken");
            }
            else
            {
                Logger.Error("No spaAuthCode or refreshToken was provided");
                return null;
            }

            Logger.Info($"Found access token in /oauth2/v2.0/token response: ");
            //AccessToken accessToken = new AccessToken(authHeader, database);
            //return accessToken;
            return null;
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

        public async Task<string> GetPrtCookie(int method, LiteDBHandler database)
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
            PrtCookie prtCookie = null;
            if (method == 0)
            {
                Logger.Info("Requesting PRT cookie from LSA/CloudAP with nonce");
                prtCookie = RequestAADRefreshToken.GetPrtCookies(database, ssoNonce).First();
            }
            else if (method == 1)
            {
                Logger.Info("Requesting PRT cookie via BrowserCore.exe with nonce");
                prtCookie = ROADToken.GetPrtCookie(database, ssoNonce);
            }
            return prtCookie.Value;
        }

        public async Task<HttpResponseMessage> SignInToService(string redirectUrl, LiteDBHandler database, string prtCookie = "", int prtMethod = 0)
        {

            // Get authorize endpoint from redirect
            string authorizeUrl = await GetAuthorizeUrl(redirectUrl);
            if (authorizeUrl is null)
                return null;

            // Parse authorize URL for client_id and scope
            ClientId = StringHandler.GetMatch(authorizeUrl, "client_id=(.*?)&");
            Scope = StringHandler.GetMatch(authorizeUrl, "scope=(.*?)&");

            if (string.IsNullOrEmpty(prtCookie))
            {
                // Get a nonce and primary refresh token cookie (x-Ms-Refreshtokencredential)
                prtCookie = await GetPrtCookie(prtMethod, database);
                if (prtCookie is null)
                    return null;
            }

            // Use PRT cookie to obtain code+id_token required for signin
            Logger.Info("Using PRT cookie with nonce to obtain code+id_token required for signin");
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
            Logger.Info($"Found portalAuthorization in DelegationToken response: {StringHandler.Truncate(portalAuthorization, 12)}");
            Logger.Debug(portalAuthorization);
            return portalAuthorization;
        }

        public async Task Authenticate(string redirectUrl, LiteDBHandler database = null, string prtCookie = "", int prtMethod = 0)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            HttpResponseMessage signinResponse = await SignInToService(redirectUrl, database, prtCookie, prtMethod);
            if (signinResponse is null) return;

            string signinResponseContent = await signinResponse.Content.ReadAsStringAsync();

            // Get tenantId for subsequent requests
            string tenantId = ParseTenantIdFromJsonResponse(signinResponseContent);
            if (tenantId is null) return;

            // Get spaAuthCode and portalAuthorization for subsequent requests
            string spaAuthCode = ParseSpaAuthCodeFromJsonResponse(signinResponseContent, database);
            string portalAuthorization = ParsePortalAuthorizationFromJsonResponse(signinResponseContent);
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

        private OAuthTokenDynamic ParseOAuthTokenFromJsonResponse(string jsonResponse, LiteDBHandler database = null)
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
            Logger.Info($"Found portal authorization (refreshToken) in response: {StringHandler.Truncate(portalAuthorization, 12)}");
            PortalAuthorization = portalAuthorization;
            Logger.Debug(portalAuthorization);
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
            Logger.Info($"Found spaAuthCode in response: {StringHandler.Truncate(spaAuthTokenBlob, 12)}");
            SpaAuthCode = spaAuthTokenBlob;
            Logger.Debug(spaAuthTokenBlob);
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
            Logger.Info($"Found tenantId in the response: {tenantId}");
            TenantId = tenantId;
            return tenantId;
        }
    }
}