using System.Threading.Tasks;

namespace Maestro
{
    public class GetAccessTokenCommand
    {
        public static async Task Execute(CommandLineOptions options, LiteDBHandler database)
        {
            var authClient = new AuthClient();
            string authRedirectUrl = "https://portal.azure.com/signin/idpRedirect.js";
            string delegationTokenUrl = "https://portal.azure.com/api/DelegationToken";

            if (!string.IsNullOrEmpty(options.PrtCookie))
            {
                Logger.Error("The --prt-cookie option is not yet implemented");
                return;
            }

            if (!string.IsNullOrEmpty(options.RefreshToken))
            {
                Logger.Error("The --refresh-token option is not yet implemented");
                return;
            }

            if (options.Method > 1 || options.Method < 0)
            {
                Logger.Error("Invalid method (-m) specified");
                CommandLine.PrintUsage("get access-token");
                return;
            }

            authClient = await AuthClient.InitAndGetAccessToken(options, database, authRedirectUrl, delegationTokenUrl);
        }
    }
}
