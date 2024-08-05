namespace Maestro
{
    public class StoreAccessTokenCommand
    {
        public static void Execute(CommandLineOptions options, LiteDBHandler database)
        {
            // Store the specified access token and exit
            if (!string.IsNullOrEmpty(options.AccessToken))
            {
                // Store the access token in the database
                var accessToken = new AccessToken(options.AccessToken, database);
                return;
            }

            Logger.Error("Missing arguments for \"store access-token\" command");
            CommandLine.PrintUsage("store access-token");
        }
    }
}
