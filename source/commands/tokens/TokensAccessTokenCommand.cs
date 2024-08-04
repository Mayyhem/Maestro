using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maestro
{
    internal class TokensAccessTokenCommand
    {
        public static async Task Execute(Dictionary<string, string> arguments, LiteDBHandler database, bool databaseOnly, bool reauth, int prtMethod)
        {
            var authClient = new AuthClient();
            string authRedirectUrl = "https://portal.azure.com/signin/idpRedirect.js";
            string delegationTokenUrl = "https://portal.azure.com/api/DelegationToken";
            string extensionName = "Microsoft_AAD_IAM";
            string requiredScope = "";
            string resourceName = "microsoft.graph";

            if (arguments.Count == 0)
            {
                Logger.Error("Missing arguments for \"access-token\" command");
                CommandLine.PrintUsage("access-token");
                return;
            }

            // Store the specified access token and exit
            if (arguments.TryGetValue("--store", out string providedAccessToken))
            {
                // Store the access token in the database
                var accessToken = new AccessToken(providedAccessToken, database);
                return;
            }

            // Request tokens
            if (arguments.TryGetValue("--prt-cookie", out string providedPrtCookie))
            {
                Logger.Error("The --prt-cookie option is not yet implemented");
                return;
            }

            if (arguments.TryGetValue("--refresh-token", out string providedRefreshToken))
            {
                Logger.Error("The --refresh-token option is not yet implemented");
                return;
            }

            if (arguments.TryGetValue("--method", out string methodString))
            {
                int.TryParse(methodString, out int accessTokenMethod);

                // Use /oauth2/v2.0/token endpoint
                if (accessTokenMethod == 0)
                {
                    if (arguments.TryGetValue("--tenant-id", out string tenantId))
                    {
                    }
                }
                else if (accessTokenMethod == 1)
                {
                    // Use /api/DelegationToken endpoint
                    if (arguments.TryGetValue("--extension", out string extension))
                    {
                        extensionName = extension;
                    }

                    if (arguments.TryGetValue("--resource", out string resource))
                    {
                        resourceName = resource;
                    }
                }
                else
                {
                    Logger.Error("Invalid method (-m) specified");
                    CommandLine.PrintUsage("access-token");
                    return;
                }

                if (!databaseOnly)
                {
                    authClient = await AuthClient.InitAndGetAccessToken(authRedirectUrl, delegationTokenUrl, extensionName, resourceName, database, providedPrtCookie, providedRefreshToken, providedAccessToken, reauth, requiredScope, prtMethod, accessTokenMethod);
                    return;
                }
            }
            // Implement show tokens
        }
    }
}
