using System.Threading.Tasks;

namespace Maestro
{
    public class ExecIntuneAppCommand
    {
        public static async Task Execute(CommandLineOptions options, LiteDBHandler database)
        {
            // Fail if neither a device or group ID is specified
            if (options.Device is null && options.Group is null)
            {
                Logger.Error("Please specify the an Intune/Entra device ID (-i) or an Entra group (-g)");
                return;
            }

            // Authenticate and get an access token for Intune
            var intuneClient = new IntuneClient();
            intuneClient = await IntuneClient.InitAndGetAccessToken(options, database);
            if (intuneClient is null) return;

            // Execute the Win32 app
            await intuneClient.ExecWin32App(options, database);
        }
    }
}
