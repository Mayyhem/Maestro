using System.Threading.Tasks;

namespace Maestro
{
    public class GetAccessTokenCommand
    {
        public static async Task Execute(CommandLineOptions options, LiteDBHandler database)
        {
            var authClient = new AuthClient(options.UserAgent, options.Proxy);
            string idpRedirectUrl = "https://portal.azure.com/signin/idpRedirect.js";
            string delegationTokenUrl = "https://portal.azure.com/api/DelegationToken";

            if (!string.IsNullOrEmpty(options.PrtCookie))
            {
                Logger.Error("The --prt-cookie option is not yet implemented");
                return;
            }

            if (!string.IsNullOrEmpty(options.RefreshToken))
            {
                // Use the /token endpoint
                options.TokenMethod = 0;

                // Authenticate and get an access token
                authClient = await AuthClient.InitAndGetAccessToken(options, database);
                return;
            }

            if (options.TokenMethod > 2 || options.TokenMethod < 0)
            {
                Logger.Error("Invalid method (-m) specified");
                CommandLine.PrintUsage("get access-token");
                return;
            }

            authClient = await AuthClient.InitAndGetAccessToken(options, database, idpRedirectUrl, delegationTokenUrl);
        }
    }
}
