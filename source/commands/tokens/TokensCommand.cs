using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maestro
{
    internal class TokensCommand
    {
        public static async Task Execute(Dictionary<string, string> arguments, LiteDBHandler database, bool databaseOnly, bool reauth, int prtMethod)
        {
            if (arguments.TryGetValue("subcommand1", out string subcommandName))
            {
                if (subcommandName == "prt-cookie")
                {
                    await TokensPrtCookieCommand.Execute(arguments, database, databaseOnly, prtMethod);
                }
                else if (subcommandName == "refresh-token")
                {
                    //await TokenRefreshCommand.Execute(arguments, database, databaseOnly, reauth);
                }
                else if (subcommandName == "access-token")
                {
                    //await TokensAccessTokenCommand.Execute(arguments, database, databaseOnly, reauth, prtMethod);
                }
            }
            else
            {
                Logger.Error("Missing arguments for \"tokens\" command");
                CommandLine.PrintUsage("tokens");
            }
        }
    }
}
