using System.Threading.Tasks;

namespace Maestro
{
    public class ExecIntuneAppCommand
    {
        public static async Task Execute(CommandLineOptions options, LiteDBHandler database)
        {
            // Authenticate and get an access token for Intune
            IntuneClient intuneClient = new IntuneClient();
            intuneClient = await IntuneClient.InitAndGetAccessToken(options, database);

            // Authenticate and get an access token for EntraID 
            var entraClient = new EntraClient();
            entraClient = await EntraClient.InitAndGetAccessToken(options, database);


        }
    }
}
