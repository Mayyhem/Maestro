using Microsoft.SqlServer.Server;
using System.Collections.Generic;
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
        private readonly IHttpHandler _httpHandler;
        public AuthClient(IHttpHandler httpHandler)
        {
            _httpHandler = httpHandler;
        }

        private async Task<string> AuthorizeWithPrt(string url, string xMSRefreshtokencredential)
        {
            string authorizeResponse = string.Empty;
            Logger.Info("Using PRT cookie to authenticate to authorize URL");

            // Set the primary refresh token header
            var prtCookie = new Cookie
            {
                Name = "X-Ms-Refreshtokencredential",
                Value = xMSRefreshtokencredential,
                Domain = "login.microsoftonline.com",
                Path = "/"
            };

            _httpHandler.CookiesContainer.Add(prtCookie);
            authorizeResponse = await _httpHandler.GetAsync(url);
            if (authorizeResponse is null)
                return Logger.NullError<string>("No response was received from the authorize URL");
            Logger.Info("Obtained response from authorize URL");
            Logger.Debug(authorizeResponse);
            return authorizeResponse;
        }

        private async Task<string> GetAccessToken(string tenantId, string portalAuthorization)
        {
            Logger.Info("Requesting access token from DelegationToken endpoint with portalAuthorization");
            string accessToken = await GetAuthHeader(tenantId, portalAuthorization);
            if (accessToken is null)
                return Logger.NullError<string>("No authHeader was found in the DelegationToken response");
            Logger.Info($"Found access token in DelegationToken response: {accessToken}");
            return accessToken;
        }

        private async Task<string> GetAuthHeader(string tenantId, string portalAuthorization)
        {
            string url = "https://intune.microsoft.com/api/DelegationToken";
            var jsonObject = new
            {
                extensionName = "Microsoft_Intune_DeviceSettings",
                resourceName = "microsoft.graph",
                tenant = tenantId,
                portalAuthorization
            };
            var serializer = new JavaScriptSerializer();
            string json = serializer.Serialize(jsonObject);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            string response = await _httpHandler.PostAsync(url, content);
            string authHeader = Util.GetMatch(response, "\"authHeader\":\"Bearer ([^\"]+)\"");
            return authHeader;
        }

        private async Task<string> GetAuthorizeUrl()
        {
            string url = "https://intune.microsoft.com/signin/idpRedirect.js";
            Logger.Info($"Requesting authorize URL from: {url}");

            string idpRedirectResponse = await _httpHandler.GetAsync(url);

            if (idpRedirectResponse is null)
            {
                Logger.Error("No response or empty response received for authorize URL request");
                return null;
            }

            Logger.Debug(idpRedirectResponse);
            string authorizeUrlPattern =
                @"https://login\.microsoftonline\.com/organizations/oauth2/v2\.0/authorize\?.*?(?=\"")";
            string authorizeUrl = Util.GetMatch(idpRedirectResponse, authorizeUrlPattern, false);
            if (authorizeUrl is null) return null;
            Logger.Info($"Found authorize URL: {authorizeUrl}");
            return authorizeUrl;
        }

        public async Task<string> GetIntuneAccessToken()
        {
            // HTTP request 1
            string authorizeUrl = await GetAuthorizeUrl();
            if (authorizeUrl is null) return null;

            // Local request to mint PRT cookie 1
            string xMsRefreshtokencredential = ROADToken.GetXMsRefreshtokencredential();
            if (xMsRefreshtokencredential is null) return null;

            // HTTP request 2
            string initialAuthorizeResponse = await AuthorizeWithPrt(authorizeUrl, xMsRefreshtokencredential);
            if (initialAuthorizeResponse is null) return null;

            // Parse response for authorize URL with nonce
            string urlToCheckForNonce = ParseInitialAuthorizeResponseForAuthorizeUrl(initialAuthorizeResponse);
            if (urlToCheckForNonce is null) return null;

            // Parse URL for nonce
            string ssoNonce = Util.GetMatch(urlToCheckForNonce, "sso_nonce=([^&]+)");
            if (ssoNonce is null)
                return Logger.NullError<string>("No sso_nonce parameter was found in the URL");
            Logger.Info($"Found sso_nonce parameter in the URL: {ssoNonce}");

            // Local request to mint PRT cookie 2 with nonce
            string xMsRefreshtokencredentialWithNonce = ROADToken.GetXMsRefreshtokencredential(ssoNonce);
            if (xMsRefreshtokencredentialWithNonce is null) return null;

            // HTTP request 3
            Logger.Info("Using PRT with nonce to obtain code+id_token required for Intune signin");
            string authorizeWithSsoNonceResponse =
                await AuthorizeWithPrt(urlToCheckForNonce, xMsRefreshtokencredentialWithNonce);
            if (authorizeWithSsoNonceResponse is null) return null;

            // Parse response for signin URL
            string actionUrl = Util.GetMatch(authorizeWithSsoNonceResponse, "<form method=\"POST\" name=\"hiddenform\" action=\"(.*?)\"");
            if (actionUrl is null)
                return Logger.NullError<string>("No hidden form action URLs were found in the response");
            Logger.Info($"Found hidden form action URL in the response: {actionUrl}");

            // Parse response for POST body with code+id_token
            FormUrlEncodedContent formData = ParseFormDataFromHtml(authorizeWithSsoNonceResponse);
            if (formData is null) return null;

            string signInResponse = await SignInToIntune(actionUrl, formData);
            if (signInResponse is null)
                return Logger.NullError<string>("Could not sign in to Intune");

            string tenantId = GetTenantIdFromJsonResponse(signInResponse);
            if (tenantId is null) return null;

            string refreshToken = GetRefreshTokenFromJsonResponse(signInResponse);
            if (refreshToken is null) return null;

            string portalAuthorization = await GetPortalAuthorization(tenantId, refreshToken);
            if (portalAuthorization is null) return null;

            Logger.Info("Requesting Intune access token");
            string intuneAccessToken = await GetAccessToken(tenantId, portalAuthorization);
            return intuneAccessToken;
        }

        // Submit refreshToken/PortalAuthorization blob to DelegationToken endpoint for PortalAuthorization blob
        private async Task<string> GetPortalAuthorization(string tenantId, string refreshToken, string url = "https://intune.microsoft.com/api/DelegationToken", string extensionName = "HubsExtension", string resourceName = "")
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
            string response = await _httpHandler.PostAsync(url, content);
            string portalAuthorization = Util.GetMatch(response, "\\\"portalAuthorization\\\":\\\"([^\\\"]+)\\\"");
            if (portalAuthorization is null)
                return Logger.NullError<string>("No portalAuthorization was found in the DelegationToken response");
            Logger.Info($"Found portalAuthorization in DelegationToken response: {Util.TruncateString(portalAuthorization)}");
            Logger.Debug(portalAuthorization);
            return portalAuthorization;
        }

        private string GetTenantIdFromJsonResponse(string jsonResponse)
        {
            // Parse response for tenantId
            string tenantId = Util.GetMatch(jsonResponse, "\\\"tenantId\\\":\\\"([^\\\"]+)\\\"");
            if (tenantId is null)
                return Logger.NullError<string>("No tenantId was found in the response");
            Logger.Info($"Found tenantId in the response: {tenantId}");
            return tenantId;
        }

        private string GetRefreshTokenFromJsonResponse(string jsonResponse)
        {
            // Parse response for refreshToken
            string refreshToken = Util.GetMatch(jsonResponse, "\\\"refreshToken\\\":\\\"([^\\\"]+)\\\"");
            if (refreshToken is null)
                return Logger.NullError<string>("No refreshToken was found in the response");
            Logger.Info($"Found refreshToken in response: {Util.TruncateString(refreshToken)}");
            Logger.Debug(refreshToken);
            return refreshToken;
        }

        private FormUrlEncodedContent ParseFormDataFromHtml(string htmlContent)
        {
            FormUrlEncodedContent formData = null;

            // Get all hidden input fields
            MatchCollection inputs = Regex.Matches(
                htmlContent, "<input type=\"hidden\" name=\"(.*?)\" value=\"(.*?)\"");

            if (inputs.Count == 0)
            {
                return Logger.NullError<FormUrlEncodedContent>("No hidden input fields were found in the response");
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
                return Logger.NullError<string>("No authorize URL was found in the response");
            }

            // Replace Unicode in URL with corresponding characters and placeholder with "organizations"
            string ssoNonceUrl = Regex.Unescape(authorizeUrlWithSsoNonceMatch.Value).Replace(
                "{0}", "organizations");
            Logger.Info($"Found authorize URL in the response: {ssoNonceUrl}");
            return ssoNonceUrl;
        }

        private async Task<string> SignInToIntune(string actionUrl, HttpContent formData)
        {
            Logger.Info("Signing in to Intune with code+id_token obtained from authorize endpoint");
            string signinResponse = await _httpHandler.PostAsync(actionUrl, formData);
            if (signinResponse is null)
                return Logger.NullError<string>("No response was received from the signin URL");
            Logger.Info("Obtained response from signin URL");
            return signinResponse;
        }
    }
}