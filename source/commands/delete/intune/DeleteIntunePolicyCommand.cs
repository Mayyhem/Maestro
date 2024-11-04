using System.Threading.Tasks;

namespace Maestro
{
    public class DeleteIntunePolicyCommand
    {
        public static async Task Execute(CommandLineOptions options, LiteDBHandler database)
        {
            IntuneClient intuneClient = await IntuneClient.InitAndGetAccessToken(options, database);
            await intuneClient.DeletePolicy(options.Id);
        }
    }
}