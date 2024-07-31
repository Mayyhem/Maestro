using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maestro
{
    public class TokenAccessCommand : ExecutedCommand
    {
        public TokenAccessCommand(Dictionary<string, string> arguments) : base(arguments)
        {
        }
        public async Task Execute()
        {
            var authClient = new AuthClient(this);
            string authRedirectUrl = "https://portal.azure.com/signin/idpRedirect.js";
            string delegationTokenUrl = "https://portal.azure.com/api/DelegationToken";
            string extensionName = "Microsoft_AAD_IAM";
            string resourceName = "microsoft.graph";
            string prtCookie = "";
            string refreshToken = "";
            string bearerToken = "";

            // Store the specified access token and exit
            if (Arguments.TryGetValue("--store", out string tokenToStore))
            {
                bearerToken = tokenToStore;
                // Need to implement
                return;
            }

            // Request tokens
            if (Arguments.TryGetValue("--prt", out string prt))
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

            if (Arguments.TryGetValue("--refresh", out string refresh))
            {
                refreshToken = refresh;
                // Need to implement    
            }

            // Request an access token with user-specified properties
            if (Arguments.TryGetValue("--extension", out string extension))
            {
                extensionName = extension;
            }

            if (Arguments.TryGetValue("--resource", out string resource))
            {
                resourceName = resource;
            }

            if (!DatabaseOnly)
            {
                //authClient = await AuthClient.InitAndGetAccessToken(this, authRedirectUrl, delegationTokenUrl, extensionName, resourceName);
            }
            else
            {
                // Implement show tokens
                return;
            }
        }
    }
}
