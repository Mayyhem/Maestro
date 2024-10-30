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

        public StaticClientWithProxyFactory(AuthClient client)
        {
            //handler = new HttpClientHandler();
            //handler.SslProtocols = SslProtocols.Tls12;
            //s_httpClient = new HttpClient(handler);
            s_httpClient = client.HttpHandler._httpClient;
        }

        public HttpClient GetHttpClient()
        {
            return s_httpClient;
        }
    }

    public class SharpGetEntraToken
    {
        public static async Task<AccessToken> Execute(AuthClient client, LiteDBHandler database)
        {
            Logger.Info("SharpGetEntraToken attempting to get an access token");
            IMsalHttpClientFactory httpClientFactory = new StaticClientWithProxyFactory(client);

            // Authority URL for Microsoft identity platform (Entra ID)
            string authority = $"https://login.microsoftonline.com/{client.TenantId}";

            // Create a PublicClientApplication instance
            var app = PublicClientApplicationBuilder.Create(client.ClientId)
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
                string[] scopes = client.Scope.Split(' ');
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