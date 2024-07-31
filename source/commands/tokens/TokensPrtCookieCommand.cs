using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maestro
{
    internal class TokensPrtCookieCommand
    {
        public static async Task Execute(Dictionary<string, string> arguments, LiteDBHandler database, bool databaseOnly, int prtMethod)
        {
            var authClient = new AuthClient();

            // Store the specified access token and exit
            if (arguments.TryGetValue("--store", out string prtCookieValue))
            {
                if (!arguments.TryGetValue("--database", out string databaseName))
                {
                    Logger.Error("Please specify the path to a database file (--database) to use the --store option");
                    CommandLine.PrintUsage("tokens");
                    return;
                }
                var _ = new PrtCookie(prtCookieValue, database);
                return;
            }
            else
            {
                Logger.Error("Missing arguments for \"prt-cookie\" command");
                CommandLine.PrintUsage("tokens");
            }

            if (!databaseOnly)
            {
                await authClient.GetPrtCookie(prtMethod, database);
            }
            else
            {
                // Need to implement show tokens
                return;
            }
        }
    }
}
