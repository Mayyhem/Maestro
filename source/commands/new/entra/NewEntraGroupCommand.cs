using System.Threading.Tasks;

namespace Maestro
{
    public class NewEntraGroupCommand
    {
        public static async Task Execute(CommandLineOptions options, LiteDBHandler database)
        {
            EntraClient entraClient = new EntraClient();
            entraClient = await EntraClient.InitAndGetAccessToken(options, database);

            await entraClient.NewGroup(options.Name, options.Member);
        }
    }
}