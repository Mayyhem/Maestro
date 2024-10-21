using System.Threading.Tasks;

namespace Maestro
{
    public class AddEntraMemberCommand
    {
        public static async Task Execute(CommandLineOptions options, LiteDBHandler database)
        {
            EntraClient entraClient = new EntraClient();
            entraClient = await EntraClient.InitAndGetAccessToken(options, database);

            await entraClient.AddGroupMember(options.Group, options.Member);
        }
    }
}