//
// Daniel Heinsen (@hotnops) is the original author of this file. Thank you, Daniel!
// https://github.com/hotnops/SharpGetEntraToken
//

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Maestro
{
    public class StaticClientWithProxyFactory : IMsalHttpClientFactory
    {
        private readonly HttpClient s_httpClient;

        //static HttpClientHandler handler;

        public StaticClientWithProxyFactory(HttpClient httpClient)
        {
            //handler = new HttpClientHandler();
            //handler.SslProtocols = SslProtocols.Tls12;
            //s_httpClient = new HttpClient(handler);
            s_httpClient = httpClient;
        }

        public HttpClient GetHttpClient()
        {
            return s_httpClient;
        }
    }

    public class SharpGetEntraToken
    {
        public static async Task<AccessToken> Execute(HttpClient httpClient, string clientId, string tenantId, string scope = "", LiteDBHandler database = null)
        {
            try
            {
                // Determine whether the current process is 64-bit or 32-bit
                bool is64BitProcess = Environment.Is64BitProcess;

                // Append the appropriate suffix to the DLL name
                string @namespace = is64BitProcess ? "costura64" : "costura32";
                string dllName = is64BitProcess ? "msalruntime.dll" : "msalruntime_x86.dll";

                // Load the unmanaged DLL
                UnmanagedDllLoader.LoadUnmanagedDll(@namespace, dllName);
                Logger.Info("MSAL DLL loaded and ready for use.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }

            Logger.Info("SharpGetEntraToken attempting to get an access token");
            IMsalHttpClientFactory httpClientFactory = new StaticClientWithProxyFactory(httpClient);

            // Authority URL for Microsoft identity platform (Entra ID)
            string authority = $"https://login.microsoftonline.com/{tenantId}";

            // Create a PublicClientApplication instance
            var app = PublicClientApplicationBuilder.Create(clientId)
                .WithAuthority(authority)
                .WithHttpClientFactory(httpClientFactory)
                .WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows))
                .Build();

            if (app == null)
            {
                Logger.Error("SharpGetEntraToken failed to initialize ");
                return null;
            }

            // Attempt to acquire token silently
            IEnumerable<IAccount> accounts = await app.GetAccountsAsync();
            if (accounts == null)
            {
                Logger.Error("SharpGetEntraToken cannot obtain accounts enumerable");
                return null;
            }

            IAccount accountToLogin = accounts.FirstOrDefault();
            if (accountToLogin == null)
            {
                accountToLogin = PublicClientApplication.OperatingSystemAccount;
            }
            try
            {
                // Split the scopes by spaces and store as string[]
                string[] scopes = scope.Split(' ');
                var result = await app.AcquireTokenSilent(scopes, accountToLogin).ExecuteAsync();
                Logger.Info($"SharpGetEntraToken got an access token:\n{result.AccessToken}");
                return new AccessToken(result.AccessToken, database);
            }
            catch (MsalUiRequiredException)
            {
                Logger.Error("SharpGetEntraToken MsalUiRequiredException: Interactive login required");
                return null;
            }
        }
    }
}