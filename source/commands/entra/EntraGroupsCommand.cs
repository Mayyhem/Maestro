using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maestro
{
    internal static class EntraGroupsCommand
    {
        public static async Task Execute(Dictionary<string, string> arguments, IDatabaseHandler database, bool databaseOnly, bool reauth)
        {
            EntraClient entraClient = new EntraClient();
            if (!databaseOnly)
            {
                entraClient = await EntraClient.InitAndGetAccessToken(database, reauth: reauth);
            }

            // Set default properties to print
            string[] properties = new string[] { };

            // User-specified properties
            if (arguments.TryGetValue("--properties", out string propertiesCsv))
            {
                properties = propertiesCsv.Split(',');
            }

            // Filter objects
            if (arguments.TryGetValue("--id", out string userId))
            {
                if (databaseOnly)
                {
                    entraClient.ShowGroups(database, properties, groupId: userId);
                    return;
                }
                await entraClient.GetGroups(groupId: userId, properties: properties, database: database);
            }
            else
            {
                // Get information for all items by default when no options are provided
                if (databaseOnly)
                {
                    entraClient.ShowGroups(database, properties);
                    return;
                }
                await entraClient.GetGroups(properties: properties, database: database);
            }
        }
    }
}
