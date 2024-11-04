using System.Threading.Tasks;

namespace Maestro
{
    public class DeleteIntuneFilterCommand
    {
        public static async Task Execute(CommandLineOptions options, LiteDBHandler database)
        {
            IntuneClient intuneClient = await IntuneClient.InitAndGetAccessToken(options, database);
            await intuneClient.DeleteDeviceAssignmentFilter(options.Id);
        }
    }
}