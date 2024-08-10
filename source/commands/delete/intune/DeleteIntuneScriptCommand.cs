using System.Threading.Tasks;

namespace Maestro
{
    public class DeleteIntuneScriptCommand
    {
        public static async Task Execute(CommandLineOptions options, LiteDBHandler database)
        {
            IntuneClient intuneClient = new IntuneClient();
            intuneClient = await IntuneClient.InitAndGetAccessToken(options, database);
            await intuneClient.DeleteScriptPackage(options.Id);
        }
    }
}
