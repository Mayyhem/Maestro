using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maestro
{
    internal class TokenAccessCommand
    {
        public static async Task Execute(Dictionary<string, string> arguments, LiteDBHandler database, bool databaseOnly)
        {
            var authClient = new AuthClient();
            string authRedirectUrl = "https://portal.azure.com/signin/idpRedirect.js";
            string delegationTokenUrl = "https://portal.azure.com/api/DelegationToken";
            string extensionName = "Microsoft_AAD_IAM";
            string resourceName = "microsoft.graph";
            string prtCookie = "";
            string refreshToken = "";
            string bearerToken = "";

            // Store the specified access token and exit
            if (arguments.TryGetValue("--store", out string tokenToStore))
            {
                bearerToken = tokenToStore;
                // Need to implement
                return;
            }

            // Request tokens
            if (arguments.TryGetValue("--prt", out string prt))
            {
                // Clear default usage text
                string defaultValue = CommandLine.commands.Find(
                    c => c.Name == "token").Subcommands.Find(s => s.Name == "access").Options.Find(o => o.LongName == "--prt").Default;
                if (prt == defaultValue)
                {
                    prtCookie = "";
                }
                else
                {
                    prtCookie = prt;
                }
            }

            if (arguments.TryGetValue("--refresh", out string refresh))
            {
                refreshToken = refresh;
                // Need to implement    
            }

            // Request an access token with user-specified properties
            if (arguments.TryGetValue("--extension", out string extension))
            {
                extensionName = extension;
            }

            if (arguments.TryGetValue("--resource", out string resource))
            {
                resourceName = resource;
            }

            if (!databaseOnly)
            {
                authClient = await AuthClient.InitAndGetAccessToken(authRedirectUrl, delegationTokenUrl, extensionName, resourceName, database, prtCookie, bearerToken);
            }
            else
            {
                // Implement show tokens
                return;
            }
        }
    }
}
