using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Threading.Tasks;

namespace Maestro
{
    public static class GetIntuneAppsCommand
    {
        public static async Task Execute(CommandLineOptions options, LiteDBHandler database)
        {
            IntuneClient intuneClient = new IntuneClient();
            intuneClient = await IntuneClient.InitAndGetAccessToken(options, database);

            // Set default properties to print
            if (options.Properties is null)
            {
                options.Properties = new List<string> {
                    "id",
                    "displayName",
                    "description",
                    "publisher",
                    "createdDateTime",
                    "lastModifiedDateTime",
                    "publishingState",
                    "isAssigned"
                };
            }

            string[] properties = options.Properties.ToArray();
            await intuneClient.GetApps(options.Id, options.Name, properties, database, true);
        }
    }
}
