using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maestro
{
    public static class GetEntraMembershipCommand
    {
        public static async Task Execute(CommandLineOptions options, LiteDBHandler database)
        {
            if (options.Id is null && options.Name is null)
            {
                Logger.Error("Please specify an object ID (-i) or name (-n)");
                CommandLine.PrintUsage($"{options.FullCommand}");
                return;
            }

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
                    "isAssignableToRole",
                    "onPremisesDomainName",
                    "onPremisesLastSyncDateTime",
                    "onPremisesNetBiosName",
                    "onPremisesSamAccountName",
                    "onPremisesSecurityIdentifier",
                    "securityIdentifier"
                };
            }

            string[] properties = options.Properties.ToArray();
            await entraClient.GetMembership(options.Id, options.Name, null, properties, database, true);
        }
    }
}
