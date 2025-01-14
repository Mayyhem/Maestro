using System.Threading.Tasks;
using System.Web.UI;

namespace Maestro
{
    public class GetAccessTokenCommand
    {
        public static async Task Execute(CommandLineOptions options, LiteDBHandler database)
        {
            var authClient = new AuthClient(options.UserAgent, options.Proxy);

            string idpRedirectUrl = $"https://{options.Target}/signin/idpRedirect.js";
            string delegationTokenUrl = $"https://{options.Target}/api/DelegationToken";

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
                await AuthClient.InitAndGetAccessToken(options, database);
                return;
            }

            if (options.TokenMethod > 2 || options.TokenMethod < 0)
            {
                Logger.Error("Invalid method (-m) specified");
                CommandLine.PrintUsage(options.FullCommand);
                return;
            }

            if (options.TokenMethod == 2)
            {
                if (string.IsNullOrEmpty(options.TenantId))
                {
                    Logger.Error("Please specify a tenant ID (-t)");
                    CommandLine.PrintUsage(options.FullCommand);
                    return;
                }
            }

            await AuthClient.InitAndGetAccessToken(options, database, idpRedirectUrl, delegationTokenUrl);
        }
    }
}
