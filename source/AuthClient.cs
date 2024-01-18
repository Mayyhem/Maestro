using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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

        private async Task<string> GetAuthorizeUrl()
        {
            Logger.Info("Requesting authorize URL from Intune");
            try
            {
                string url = "https://intune.microsoft.com/signin/idpRedirect.js";
                string idpRedirectResponse = await _httpHandler.GetAsync(url);

                if (idpRedirectResponse != null)
                {
                    Logger.Debug(idpRedirectResponse);

                    string pattern = @"https://login\.microsoftonline\.com/organizations/oauth2/v2\.0/authorize\?.*?(?=\"")";

                    Match idpRedirectUrlMatch = Regex.Match(idpRedirectResponse, pattern);

                    if (idpRedirectUrlMatch.Success)
                    {
                        Logger.Info($"Found authorize URL: {idpRedirectUrlMatch.Value}");
                    }
                    else
                    {
                        Logger.Warning("No authorize URL found in the response");
                    }
                    return idpRedirectUrlMatch.Value;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
            }
            return null;
        }

        private string ParseInitialAuthorizeResponseForSsoNonceUrl(string authorizeResponse)
        {
            string pattern = "(?<=\"urlTenantedEndpointFormat\":\")(https:\\/\\/[^\",]+)";
            Match authorizeUrlWithSsoNonceMatch = Regex.Match(authorizeResponse, pattern);

            if (!authorizeUrlWithSsoNonceMatch.Success)
            {
                Logger.Warning("No authorize URL found in the response");
                return null;
            }
            // Replace Unicode in URL with corresponding characters and placeholder with "organizations"
            string ssoNonceUrl = Regex.Unescape(authorizeUrlWithSsoNonceMatch.Value).Replace(
                "{0}", "organizations");
            Logger.Info($"Found authorize URL in the response: {ssoNonceUrl}");
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
                Logger.Info($"Found hidden form action URL in the response: {actionUrl}");
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
                Logger.Info("Found hidden input fields in the response");
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
                Logger.Warning("No sso_nonce parameter was found in the URL");
                return null;
            }
            string ssoNonce = ssoNonceMatch.Groups[1].Value;
            Logger.Info($"Found sso_nonce parameter in the URL: {ssoNonce}");
            return ssoNonce;
        }

        public async Task<string> GetIntuneAccessToken()
        {
            try
            {
                string authorizeUrl = await Util.ExecuteAndCheckForNullAsync(
                    GetAuthorizeUrl,
                    nameof(GetAuthorizeUrl));

                Logger.Info("Requesting PRT cookie for this user/device from LSA/CloudAP");
                string xMsRefreshtokencredential = ROADToken.GetXMsRefreshtokencredential();

                Logger.Info("Using PRT to authenticate to authorize URL");
                string initialAuthorizeResponse = await Util.ExecuteAndCheckForNullAsync(
                    () => AuthorizeWithPrt(authorizeUrl, xMsRefreshtokencredential),
                    $"First call to {nameof(AuthorizeWithPrt)}");
                
                string authorizeUrlWithSsoNonce = Util.ExecuteAndCheckForNull(
                    () => ParseInitialAuthorizeResponseForSsoNonceUrl(initialAuthorizeResponse),
                    nameof(ParseInitialAuthorizeResponseForSsoNonceUrl));
                
                string ssoNonce = Util.ExecuteAndCheckForNull(
                    () => ParseSsoNonce(authorizeUrlWithSsoNonce),
                    nameof(ParseSsoNonce));

                Logger.Info("Requesting PRT cookie using Azure-supplied nonce");
                string xMsRefreshtokencredentialWithNonce = Util.ExecuteAndCheckForNull(
                    () => ROADToken.GetXMsRefreshtokencredential(ssoNonce),
                    nameof(ROADToken.GetXMsRefreshtokencredential));

                Logger.Info("Using PRT with nonce to obtain code+id_token required for Intune signin");
                string authorizeWithSsoNonceResponse = await Util.ExecuteAndCheckForNullAsync(
                    () => AuthorizeWithPrt(authorizeUrlWithSsoNonce, xMsRefreshtokencredentialWithNonce),
                    $"Second call to {nameof(AuthorizeWithPrt)} with sso_nonce");

                (string actionUrl, FormUrlEncodedContent formData) = Util.ExecuteAndCheckForNull(
                    () => ParseFormDataFromHtml(authorizeWithSsoNonceResponse),
                    nameof(ParseFormDataFromHtml));

                Logger.Info("Signing in to Intune with code+id_token obtained from authorize endpoint");
                string signinResponse = await Util.ExecuteAndCheckForNull(
                    () => _httpHandler.PostAsync(actionUrl, formData),
                    nameof(_httpHandler.PostAsync));
                Logger.Debug(signinResponse);

                /*
                string refreshToken = Util.ExecuteAndCheckForNull(
                    () => ParseRefreshToken(signinResponse),
                    nameof(ParseRefreshToken));
                */
                string refreshToken = Util.ExecuteAndCheckForNull(
                    () => Util.GetMatch(signinResponse, "\\\"refreshToken\\\":\\\"([^\\\"]+)\\\""),
                    "");
                Logger.Debug($"Found refresh token after signin:\n{refreshToken}");
            }
            catch (Exception e)
            {
                Logger.Error(e.Message + e.StackTrace);
            }
            return null;
        }
    }
}