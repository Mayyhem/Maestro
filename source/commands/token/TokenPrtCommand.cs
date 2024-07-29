using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maestro
{
    internal class TokenPrtCommand
    {
        public static async Task Execute(Dictionary<string, string> arguments, LiteDBHandler database, bool databaseOnly)
        {
            var authClient = new AuthClient();
            string prtCookie = "";

            // Store the specified access token and exit
            if (arguments.TryGetValue("--store", out string tokenToStore))
            {
                prtCookie = tokenToStore;
                // Need to implement
                return;
            }

            if (!databaseOnly)
            {
                await authClient.GetPrtCookie();
                // Need to implement database components
            }
            else
            {
                // Need to implement show tokens
                return;
            }
        }
    }
}
