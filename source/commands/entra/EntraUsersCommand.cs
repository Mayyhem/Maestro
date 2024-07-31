using Microsoft.SqlServer.Server;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maestro
{
    public class EntraUsersCommand : EntraCommand
    {
        // Set default properties to print
        public string[] Properties { get; private set; }
        public string UserId { get; private set; }

        /*
        public EntraUsersCommand(Dictionary<string, string> arguments) : base(arguments)
        {
            if (Arguments.TryGetValue("--id", out string userId))
            {
                UserId = userId;
            }

            // User-specified properties
            if (Arguments.TryGetValue("--properties", out string propertiesCsv))
            {
                Properties = propertiesCsv.Split(',');
            }
        }
        */
        public async Task Execute()
        {
            EntraClient entraClient = new EntraClient(this);

            if (!DatabaseOnly)
            {
                await entraClient.InitAndGetAccessToken();
            }

            // Filter objects
            if (!string.IsNullOrEmpty(UserId))
            {
                if (DatabaseOnly)
                {
                    entraClient.ShowUsers(this);
                    return;
                }
                await entraClient.GetUser(UserId, properties: Properties, database: Database);
            }
            else
            {
                // Get information for all items by default when no options are provided
                if (DatabaseOnly)
                {
                    entraClient.ShowUsers(this);
                    return;
                }
                await entraClient.GetUsers(properties: Properties, database: Database);
            }
        }
    }
}
