using System.Linq;

namespace Maestro
{
    public class ShowAccessTokensCommand
    {
        public static void Execute(CommandLineOptions options, LiteDBHandler database)
        {
            // Show all access tokens in the database
            var accessTokens = database.Query("AccessToken", options);

            if (accessTokens == null)
            {
                Logger.Info("No access tokens found in the database.");
                return;
            }

            Logger.InfoTextOnly(accessTokens.ToJson(options.Raw));
        }
    }
}
