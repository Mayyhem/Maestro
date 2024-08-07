using System.Threading.Tasks;

namespace Maestro
{
    public class ExecIntuneDeviceQueryCommand
    {
        public static async Task Execute(CommandLineOptions options, LiteDBHandler database)
        {
            // Authenticate and get an access token for Intune
            var intuneClient = new IntuneClient();
            intuneClient = await IntuneClient.InitAndGetAccessToken(options, database);
            await intuneClient.ExecuteDeviceQuery(options.Query, options.Retries, options.Wait, options.Id, options.Name, database);
        }
    }
}
