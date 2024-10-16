using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maestro
{
    public static class GetEntraGroupsCommand
    {
        public static async Task Execute(CommandLineOptions options, LiteDBHandler database)
        {
            EntraClient entraClient = new EntraClient();
            entraClient = await EntraClient.InitAndGetAccessToken(options, database);
            if (entraClient is null) return;

            // Set default properties to print
            if (options.Properties is null)
            {
                options.Properties = new List<string> {
                    "id",
                    "description",
                    "displayName",
                    "onPremisesDomainName",
                    "onPremisesLastSyncDateTime",
                    "onPremisesNetBiosName",
                    "onPremisesSamAccountName",
                    "onPremisesSecurityIdentifier",
                    "onPremisesSyncEnabled",
                    "securityEnabled",
                    "securityIdentifier",
                };
            }

            string[] properties = options.Properties.ToArray();
            await entraClient.GetGroups(options.Id, properties, database, true);
        }
    }
}
