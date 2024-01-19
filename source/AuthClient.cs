using System;
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
            try
            {
                // Set the primary refresh token header
                var prtCookie = new Cookie
                {
                    Name = "X-Ms-Refreshtokencredential",
                    Value = xMSRefreshtokencredential,
                    Domain = "login.microsoftonline.com",
                    Path = "/"
                };

                _httpHandler.CookiesContainer.Add(prtCookie);

                string authorizeResponse =
                    await _httpHandler.GetAsync(url);

                if (authorizeResponse != null)
                {
                    Logger.Debug(authorizeResponse);
                    return authorizeResponse;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
            }
            return null;
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
            try
            {
                string url = "https://intune.microsoft.com/signin/idpRedirect.js";
                string idpRedirectResponse = await _httpHandler.GetAsync(url);

                if (idpRedirectResponse != null)
                {
                    Logger.Debug(idpRedirectResponse);
                    string pattern = @"https://login\.microsoftonline\.com/organizations/oauth2/v2\.0/authorize\?.*?(?=\"")";
                    Match idpRedirectUrlMatch = Regex.Match(idpRedirectResponse, pattern);
                    return idpRedirectUrlMatch.Value;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
            }
            return null;
        }

        // Submit refreshToken to DelegationToken endpoint for PortalAuthorization blob
        private async Task<string> GetPortalAuthorization(string tenantId, string refreshToken)
        {
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

        private string ParseInitialAuthorizeResponseForSsoNonceUrl(string authorizeResponse)
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


        // Parse PortalAuthorization values from Delegation endpoint responses
        private string ParsePortalAuthorization(string response)
        {
            // Second request returns the value we want in PortalAuthorization key
            Match portalAuthorizationMatch = Regex.Match(
                response, "\\\"PortalAuthorization\\\":\\\"([^\\\"]+)\\\"");

            if (portalAuthorizationMatch.Success)
            {
                string portalAuthorization = portalAuthorizationMatch.Groups[1].Value;
                Logger.Info($"Found PortalAuthorization in the response");
                Logger.Debug(portalAuthorization);
                return portalAuthorization;
            }
            Logger.Warning("No PortalAuthorization was found in the response");
            return null;
        }

        // Parse refreshToken values from DelegationToken endpoint responses
        private string ParseRefreshToken(string response)
        {
            // First request returns the value we want in refreshToken key
            Match refreshTokenMatch = Regex.Match(
                response, "\\\"refreshToken\\\":\\\"([^\\\"]+)\\\"");
            if (refreshTokenMatch.Success)
            {
                string refreshToken = refreshTokenMatch.Groups[1].Value;
                Logger.Info($"Found refreshToken in the response");
                Logger.Debug(refreshToken);
                return refreshToken;
            }
            Logger.Warning("No refreshToken was found in the response");
            return null;
        }
    
        
        private string ParseSsoNonce(string url)
        {
            string pattern = "sso_nonce=([^&]+)";
            Match ssoNonceMatch = Regex.Match(url, pattern);

            if (!ssoNonceMatch.Success)
            {
                return null;
            }
            string ssoNonce = ssoNonceMatch.Groups[1].Value;
            return ssoNonce;
        }

        public async Task<string> GetIntuneAccessToken()
        {
            try
            {
                // HTTP request 1
                Logger.Info("Requesting authorize URL from Intune");
                string authorizeUrl = await GetAuthorizeUrl(); 
                if (authorizeUrl == null)
                {
                    Logger.Error("No authorize URL found in the response");
                    return null;
                }
                Logger.Info($"Found authorize URL: {authorizeUrl}");

                // Local request to mint PRT cookie 1
                Logger.Info("Requesting PRT cookie for this user/device from LSA/CloudAP");
                string xMsRefreshtokencredential = ROADToken.GetXMsRefreshtokencredential();
                if (xMsRefreshtokencredential == null)
                {
                    Logger.Error("Failed to obtain x-Ms-Refreshtokencredential");
                    return null;
                }
                Logger.Info($"Obtained x-Ms-Refreshtokencredential: {xMsRefreshtokencredential}");

                // HTTP request 2
                Logger.Info("Using PRT cookie to authenticate to authorize URL");
                string initialAuthorizeResponse = await AuthorizeWithPrt(authorizeUrl, xMsRefreshtokencredential);
                if (initialAuthorizeResponse == null)
                {
                    Logger.Error("No response was received from the authorize URL");
                    return null;
                }
                Logger.Info("Obtained response from authorize URL");

                string ssoNonceUrl = ParseInitialAuthorizeResponseForSsoNonceUrl(initialAuthorizeResponse);
                if (ssoNonceUrl == null)
                {
                    Logger.Warning("No authorize URL was found in the response");
                    return null;
                }
                Logger.Info($"Found authorize URL in the response: {ssoNonceUrl}");
                
                string ssoNonce = ParseSsoNonce(ssoNonceUrl);
                if (ssoNonce == null)
                {
                    Logger.Warning("No sso_nonce parameter was found in the URL");
                    return null;
                }
                Logger.Info($"Found sso_nonce parameter in the URL: {ssoNonce}");

                // Local request to mint PRT cookie 2
                Logger.Info("Requesting PRT cookie from LSA/CloudAP using nonce");
                string xMsRefreshtokencredentialWithNonce = ROADToken.GetXMsRefreshtokencredential(ssoNonce);
                if (xMsRefreshtokencredentialWithNonce == null)
                {
                    Logger.Error("Failed to obtain x-Ms-Refreshtokencredential with nonce");
                    return null;
                }
                Logger.Info($"Obtained x-Ms-Refreshtokencredential with nonce: {xMsRefreshtokencredentialWithNonce}");

                // HTTP request 3
                Logger.Info("Using PRT with nonce to obtain code+id_token required for Intune signin");
                string authorizeWithSsoNonceResponse =
                    await AuthorizeWithPrt(ssoNonceUrl, xMsRefreshtokencredentialWithNonce);
                if (authorizeWithSsoNonceResponse == null)
                {
                    Logger.Error("No response was received from the authorize URL");
                    return null;
                }
                Logger.Info("Obtained response from authorize URL");

                (string actionUrl, FormUrlEncodedContent formData) = ParseFormDataFromHtml(authorizeWithSsoNonceResponse);
                if (actionUrl == null)
                {
                    Logger.Error("No hidden form action URLs were found in the response");
                    return null;
                }
                Logger.Info($"Found hidden form action URL in the response: {actionUrl}");

                if (formData == null)
                {
                    Logger.Error("No hidden input fields were found in the response");
                    return null;
                }
                Logger.Info("Found hidden input fields in the response");

                // HTTP request 4
                Logger.Info("Signing in to Intune with code+id_token obtained from authorize endpoint");
                string signinResponse = await _httpHandler.PostAsync(actionUrl, formData);

                string tenantId = Util.GetMatch(signinResponse, "\\\"tenantId\\\":\\\"([^\\\"]+)\\\"");
                if (tenantId == null)
                {
                    Logger.Error("No tenantId was found in the signin response");
                    return null;
                }
                Logger.Info($"Found tenantId in signin response: {tenantId}");

                string refreshToken = Util.GetMatch(signinResponse, "\\\"refreshToken\\\":\\\"([^\\\"]+)\\\"");
                if (refreshToken == null)
                {
                    Logger.Error("No refreshToken was found in the signin response");
                    return null;
                }
                Logger.Info($"Found refresh token in signin response:\n{refreshToken}");

                // HTTP request 5
                Logger.Info("Requesting portalAuthorization from DelegationToken endpoint with refreshToken");
                string portalAuthorization = await GetPortalAuthorization(tenantId, refreshToken);
                if (portalAuthorization == null)
                {
                    Logger.Error("No portalAuthorization was found in the DelegationToken response");
                    return null;
                }
                Logger.Info("Obtained portalAuthorization from DelegationToken response");

                // HTTP request 6
                Logger.Info("Requesting Intune access token from DelegationToken endpoint with portalAuthorization");
                string accessToken = await GetAuthHeader(tenantId, portalAuthorization);
                if (accessToken == null)
                {
                    Logger.Error("No authHeader was found in the DelegationToken response");
                    return null;
                }
                Logger.Info($"Obtained Intune access token: {accessToken}");
                return accessToken;
            }
            catch (Exception e)
            {
                Logger.Error(e.Message + e.StackTrace);
            }
            return null;
        }
    }
}