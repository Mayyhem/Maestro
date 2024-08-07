using System.Threading.Tasks;

namespace Maestro
{
    public class ExecIntuneSyncCommand
    {
        public static async Task Execute(CommandLineOptions options, LiteDBHandler database)
        {
            // Authenticate and get an access token for Intune
            var intuneClient = new IntuneClient();
            intuneClient = await IntuneClient.InitAndGetAccessToken(options, database);
            await intuneClient.SyncDevice(options.Id, database);
        }
    }
}
