using System.Threading.Tasks;

namespace Maestro
{
    public class GetPrtCookieCommand
    {
        public static async Task Execute(CommandLineOptions options, LiteDBHandler database)
        {
            var authClient = new AuthClient();
            await authClient.GetPrtCookie(options.PrtMethod, database);
        }
    }
}
