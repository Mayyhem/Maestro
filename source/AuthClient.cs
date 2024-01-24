using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Policy;
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
            Logger.Debug(authorizeResponse);
            return authorizeResponse;
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
            return authorizeUrl;
        }

        // Submit refreshToken to DelegationToken endpoint for PortalAuthorization blob
        private async Task<string> GetPortalAuthorization(string tenantId, string refreshToken)
        {
            Logger.Info("Requesting portalAuthorization from DelegationToken endpoint with refreshToken");
            string url = "https://intune.microsoft.com/api/DelegationToken";
            var jsonObject = new
            {
                extensionName = "HubsExtension",
                resourceName = "",
                tenant = tenantId,
                portalAuthorization = refreshToken
            };
            var serializer = new JavaScriptSerializer();
            string json = serializer.Serialize(jsonObject);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            string response = await _httpHandler.PostAsync(url, content);
            string portalAuthorization = Util.GetMatch(response, "\\\"portalAuthorization\\\":\\\"([^\\\"]+)\\\"");
            return portalAuthorization;
        }

        private string ParseInitialAuthorizeResponseForAuthorizeUrl(string authorizeResponse)
        {
            string pattern = "(?<=\"urlTenantedEndpointFormat\":\")(https:\\/\\/[^\",]+)";
            Match authorizeUrlWithSsoNonceMatch = Regex.Match(authorizeResponse, pattern);

            if (!authorizeUrlWithSsoNonceMatch.Success)
            {
                return null;
            }
            // Replace Unicode in URL with corresponding characters and placeholder with "organizations"
            string ssoNonceUrl = Regex.Unescape(authorizeUrlWithSsoNonceMatch.Value).Replace(
                "{0}", "organizations");
            return ssoNonceUrl;
        }

        private (string, FormUrlEncodedContent) ParseFormDataFromHtml(string htmlContent)
        {
            string actionUrl = null;
            FormUrlEncodedContent formData = null;

            // Get form action URL
            Match actionUrlMatch = Regex.Match(
                htmlContent, "<form method=\"POST\" name=\"hiddenform\" action=\"(.*?)\"");

            if (actionUrlMatch.Success)
            {
                actionUrl = actionUrlMatch.Groups[1].Value;
            }

            // Get all hidden input fields
            MatchCollection inputs = Regex.Matches(
                htmlContent, "<input type=\"hidden\" name=\"(.*?)\" value=\"(.*?)\"");

            if (inputs.Count > 0)
            {
                var formDataPairs = new Dictionary<string, string>();
                foreach (Match input in inputs)
                {
                    formDataPairs[input.Groups[1].Value] = input.Groups[2].Value;
                }
                // Construct POST Request Data
                formData = new FormUrlEncodedContent(formDataPairs);
            }
            return (actionUrl, formData);
        }

        public async Task<string> GetIntuneAccessToken()
        {
            // HTTP request 1
            string authorizeUrl = await GetAuthorizeUrl();
            if (authorizeUrl is null) 
                return Logger.NullError("No authorize URL found in the response");
            Logger.Info($"Found authorize URL: {authorizeUrl}");

            // Local request to mint PRT cookie 1
            string xMsRefreshtokencredential = ROADToken.GetXMsRefreshtokencredential();
            if (xMsRefreshtokencredential is null)
                return Logger.NullError("Failed to obtain x-Ms-Refreshtokencredential");
            Logger.Info($"Obtained x-Ms-Refreshtokencredential: {xMsRefreshtokencredential}");

            // HTTP request 2
            string initialAuthorizeResponse = await AuthorizeWithPrt(authorizeUrl, xMsRefreshtokencredential);
            if (initialAuthorizeResponse is null)
                return Logger.NullError("No response was received from the authorize URL");
            Logger.Info("Obtained response from authorize URL");

            // Parse response for authorize URL with nonce
            string urlToCheckForNonce = ParseInitialAuthorizeResponseForAuthorizeUrl(initialAuthorizeResponse);
            if (urlToCheckForNonce is null) 
                return Logger.NullError("No authorize URL was found in the response");
            Logger.Info($"Found authorize URL in the response: {urlToCheckForNonce}");

            // Parse URL for nonce
            string ssoNonce = Util.GetMatch(urlToCheckForNonce, "sso_nonce=([^&]+)");
            if (ssoNonce is null) 
                return Logger.NullError("No sso_nonce parameter was found in the URL");
            Logger.Info($"Found sso_nonce parameter in the URL: {ssoNonce}");

            // Local request to mint PRT cookie 2 with nonce
            string xMsRefreshtokencredentialWithNonce = ROADToken.GetXMsRefreshtokencredential(ssoNonce);
            if (xMsRefreshtokencredentialWithNonce is null) 
                return Logger.NullError("Failed to obtain x-Ms-Refreshtokencredential with nonce");
            Logger.Info($"Obtained x-Ms-Refreshtokencredential with nonce: {xMsRefreshtokencredentialWithNonce}");

            // HTTP request 3
            Logger.Info("Using PRT with nonce to obtain code+id_token required for Intune signin");
            string authorizeWithSsoNonceResponse =
                await AuthorizeWithPrt(urlToCheckForNonce, xMsRefreshtokencredentialWithNonce);
            if (authorizeWithSsoNonceResponse is null) 
                return Logger.NullError("No response was received from the authorize URL");
            Logger.Info("Obtained response from authorize URL");

            // Parse response for signin URL and POST body
            (string actionUrl, FormUrlEncodedContent formData) = ParseFormDataFromHtml(authorizeWithSsoNonceResponse);
            if (actionUrl is null) 
                return Logger.NullError("No hidden form action URLs were found in the response");
            Logger.Info($"Found hidden form action URL in the response: {actionUrl}");

            if (formData is null) 
                return Logger.NullError("No hidden input fields were found in the response");
            Logger.Info("Found hidden input fields in the response");

            // HTTP request 4
            Logger.Info("Signing in to Intune with code+id_token obtained from authorize endpoint");
            string signinResponse = await _httpHandler.PostAsync(actionUrl, formData);
            if (signinResponse is null) 
                return Logger.NullError("No response was received from the signin URL");
            Logger.Info("Obtained response from signin URL");

            // Parse response for tenantId and refreshToken
            string tenantId = Util.GetMatch(signinResponse, "\\\"tenantId\\\":\\\"([^\\\"]+)\\\"");
            if (tenantId is null) 
                return Logger.NullError("No tenantId was found in the signin response");
            Logger.Info($"Found tenantId in signin response: {tenantId}");

            string refreshToken = Util.GetMatch(signinResponse, "\\\"refreshToken\\\":\\\"([^\\\"]+)\\\"");
            if (refreshToken is null)
                return Logger.NullError("No refreshToken was found in the signin response");
            Logger.Info($"Found refresh token in signin response:\n{refreshToken}");

            // HTTP request 5
            string portalAuthorization = await GetPortalAuthorization(tenantId, refreshToken);
            if (portalAuthorization is null)
                return Logger.NullError("No portalAuthorization was found in the DelegationToken response");
            Logger.Info($"Found portalAuthorization in DelegationToken response: {portalAuthorization}");

            // HTTP request 6
            Logger.Info("Requesting Intune access token from DelegationToken endpoint with portalAuthorization");
            string intuneAccessToken = await GetAuthHeader(tenantId, portalAuthorization);
            if (intuneAccessToken is null)
                return Logger.NullError("No authHeader was found in the DelegationToken response");
            Logger.Info($"Found Intune access token in DelegationToken response: {intuneAccessToken}");
            return intuneAccessToken;
        }
    }
}