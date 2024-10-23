using System.Threading.Tasks;

namespace Maestro
{
    public class DeleteIntuneAppCommand
    {
        public static async Task Execute(CommandLineOptions options, LiteDBHandler database)
        {
            IntuneClient intuneClient = new IntuneClient();
            intuneClient = await IntuneClient.InitAndGetAccessToken(options, database);
            await intuneClient.DeleteApplication(options.Id);
        }
    }
}