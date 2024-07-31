using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Maestro
{
    public class EntraGroupsCommand : Cloneable
    {
        // Set default properties to print
        public string[] Properties { get; private set; }
        public string GroupId { get; private set; }

        // ExecutedCommand properties
        public string Command { get; private set; }
        public Dictionary<string, string> Arguments { get; private set; }
        public Logger.LogLevel LogLevel { get; private set; } = Logger.LogLevel.Info;
        public LiteDBHandler Database { get; private set; }
        public bool Reauth { get; private set; }
        public bool DatabaseOnly { get; private set; }
        public int PrtMethod { get; private set; }

        // EntraCommand properties
        public string Subcommand { get; set; }

        // Parameterless constructor required for cloning properties
        public EntraGroupsCommand() { }

        public EntraGroupsCommand(EntraCommand entraCommand, Dictionary<string, string> arguments)
        {
            // Clone the matching properties of the parent commands
            entraCommand.CloneTo<EntraGroupsCommand>(this);

            if (Arguments.TryGetValue("--id", out string groupId))
            {
                GroupId = groupId;
            }

            if (Arguments.TryGetValue("--properties", out string propertiesCsv))
            {
                Properties = propertiesCsv.Split(',');
            }
        }

        public async Task Execute(EntraCommand entraCommand, Dictionary<string, string> arguments)
        {
            var entraGroupsCommand = new EntraGroupsCommand(entraCommand, arguments);

            EntraClient entraClient = new EntraClient(entraGroupsCommand);

            if (!DatabaseOnly)
            {
                entraClient = await entraClient.InitAndGetAccessToken();
            }

            // Filter objects
            if (!string.IsNullOrEmpty(GroupId))
            {
                if (DatabaseOnly)
                {
                    entraClient.ShowGroups(this);
                    return;
                }
                await entraClient.GetGroups(GroupId, properties: Properties, database: Database);
            }
            else
            {
                // Get information for all items by default when no options are provided
                if (DatabaseOnly)
                {
                    entraClient.ShowGroups(this);
                    return;
                }
                await entraClient.GetGroups(properties: Properties, database: Database);
            }
        }
    }
}
