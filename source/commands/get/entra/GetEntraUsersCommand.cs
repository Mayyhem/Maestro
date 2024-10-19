using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maestro
{
    public class GetEntraUsersCommand
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
                    "accountEnabled",
                    "createdDateTime",
                    "displayName",
                    "onPremisesDomainName",
                    "onPremisesLastSyncDateTime",
                    "onPremisesSecurityIdentifier",
                    "onPremisesSamAccountName",
                    "onPremisesSyncEnabled",
                    "onPremisesUserPrincipalName",
                    "securityIdentifier",
                    "userPrincipalName",
                    "userType",
                    "identities",
                };
            }

            string[] properties = options.Properties.ToArray();
            await entraClient.GetUsers(options.Id, options.Name, properties, database, true);
        }
    }
}
