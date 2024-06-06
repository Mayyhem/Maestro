using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Xml;

namespace Maestro
{
    public class AuthClient : IAuthClient
    {
        public IHttpHandler HttpHandler { get; }
        public string RefreshToken { get; private set; }
        public string TenantId { get; private set; }

        public AuthClient(IHttpHandler httpHandler)
        {
            HttpHandler = httpHandler;
        }

        private async Task<HttpResponseMessage> AuthorizeWithPrt(string url, string xMSRefreshtokencredential)
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

        public async Task<string> GetAccessToken(string tenantId, string portalAuthorization, string url, string extensionName,
            string resourceName)
        {
            Logger.Info("Requesting access token from DelegationToken endpoint with portalAuthorization");
            string accessToken = await GetAuthHeader(tenantId, portalAuthorization, url, extensionName, resourceName);
            if (accessToken is null)
            {
                Logger.Error("No authHeader was found in the DelegationToken response");
                return null;
            }
            Logger.Info($"Found access token in DelegationToken response: {accessToken}");
            return accessToken;
        }

        private async Task<string> GetAuthHeader(string tenantId, string portalAuthorization, string url, string extensionName, 
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
            HttpResponseMessage response = await HttpHandler.PostAsync(url, content);
            string responseContent = await response.Content.ReadAsStringAsync();
            string authHeader = StringHandler.GetMatch(responseContent, "\"authHeader\":\"Bearer ([^\"]+)\"");
            return authHeader;
        }

        private async Task<string> GetAuthorizeUrl()
        {
            string redirectUrl = "https://intune.microsoft.com/signin/idpRedirect.js";
            Logger.Info($"Requesting authorize URL from: {redirectUrl}");

            HttpResponseMessage idpRedirectResponse = await HttpHandler.GetAsync(redirectUrl);
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

        public async Task<string> GetEntraIdPortalAuthorization(string tenantId, string portalAuthorization)
        {
            Logger.Info("Requesting EntraID portal authorization");
            string entraIdPortalAuthorization = await GetPortalAuthorization(tenantId, portalAuthorization,
                "https://intune.microsoft.com/api/DelegationToken",
                "Microsoft_AAD_IAM", "microsoft.graph");
            if (entraIdPortalAuthorization is null) return null;

            Logger.Info("Received EntraID portal authorization");
            RefreshToken = entraIdPortalAuthorization;
            return entraIdPortalAuthorization;
        }

        private async Task<HttpResponseMessage> SignInToIntune()
        {
            // HTTP request 1
            string authorizeUrl = await GetAuthorizeUrl();
            if (authorizeUrl is null) 
                return null;

            // Local request to mint PRT cookie 1
            string xMsRefreshtokencredential = ROADToken.GetXMsRefreshtokencredential();
            if (xMsRefreshtokencredential is null) 
                return null;

            // HTTP request 2
            HttpResponseMessage initialAuthorizeResponse = await AuthorizeWithPrt(authorizeUrl, xMsRefreshtokencredential);
            if (initialAuthorizeResponse is null) 
                return null;
            string initialAuthorizeResponseContent = await initialAuthorizeResponse.Content.ReadAsStringAsync();

            // Parse response for authorize URL with nonce
            string urlToCheckForNonce = ParseInitialAuthorizeResponseForAuthorizeUrl(initialAuthorizeResponseContent);
            if (urlToCheckForNonce is null) 
                return null;

            // Parse URL for nonce
            string ssoNonce = StringHandler.GetMatch(urlToCheckForNonce, "sso_nonce=([^&]+)");
            if (ssoNonce is null)
            {
                Logger.Error("No sso_nonce parameter was found in the URL");
                return null;
            }
            Logger.Info($"Found sso_nonce parameter in the URL: {ssoNonce}");

            // Local request to mint PRT cookie 2 with nonce
            string xMsRefreshtokencredentialWithNonce = ROADToken.GetXMsRefreshtokencredential(ssoNonce);
            if (xMsRefreshtokencredentialWithNonce is null) 
                return null;

            // HTTP request 3
            Logger.Info("Using PRT with nonce to obtain code+id_token required for Intune signin");
            HttpResponseMessage authorizeWithSsoNonceResponse = await AuthorizeWithPrt(urlToCheckForNonce, xMsRefreshtokencredentialWithNonce);
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

            Logger.Info("Signing in to Intune with code+id_token obtained from authorize endpoint");
            HttpResponseMessage signinResponse = await HttpHandler.PostAsync(actionUrl, formData);
            if (signinResponse is null)
            {
                Logger.Error("Could not sign in to Intune");
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

        public async Task<(string, string)> GetTenantIdAndRefreshToken(IDatabaseHandler database = null)
        {
            HttpResponseMessage signinResponse = await SignInToIntune();
            if (signinResponse is null) return (null, null);
            string signinResponseContent = await signinResponse.Content.ReadAsStringAsync();

            string tenantId = ParseTenantIdFromJsonResponse(signinResponseContent);
            if (tenantId is null) return (null, null);
            TenantId = tenantId;

            OAuthToken oAuthToken = ParseOAuthTokenFromJsonResponse(signinResponseContent, database);
            if (oAuthToken is null) return (null, null);
            var test = database.FindValidOAuthToken();

            string refreshToken = ParseRefreshTokenFromJsonResponse(signinResponseContent);
            if (refreshToken is null) return (null, null);
            RefreshToken = refreshToken;

            return (tenantId, refreshToken);
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