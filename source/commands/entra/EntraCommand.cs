using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maestro
{
    public class EntraCommand : Cloneable
    {
        public string Subcommand { get; set; }

        // ExecutedCommand properties
        public string Command { get; private set; }
        public Dictionary<string, string> Arguments { get; private set; }
        public Logger.LogLevel LogLevel { get; private set; } = Logger.LogLevel.Info;
        public LiteDBHandler Database { get; private set; }
        public bool Reauth { get; private set; }
        public bool DatabaseOnly { get; private set; }
        public int PrtMethod { get; private set; }

        // Parameterless constructor required for cloning parent commands
        public EntraCommand() { }

        public EntraCommand(ExecutedCommand executedCommand, Dictionary<string, string> arguments)
        {
            // Clone the properties of the parent commands
            executedCommand.CloneTo<EntraCommand>(this);
        }

        public async Task Execute(ExecutedCommand executedCommand, Dictionary<string, string> arguments)
        {
            var entraCommand = new EntraCommand(executedCommand, arguments);

            if (Arguments.TryGetValue("subcommand1", out string subcommandName))
            {
                Subcommand = subcommandName;

                if (Subcommand == "devices")
                {

                    //await DevicesCommand.Execute(arguments, database);
                }
                else if (Subcommand == "groups")
                {
                    EntraGroupsCommand entraGroupsCommand = new EntraGroupsCommand(entraCommand, arguments);
                    await entraGroupsCommand.Execute(this, arguments);
                }
                else if (Subcommand == "users")
                {
                    //await EntraUsersCommand.Execute(arguments, database, databaseOnly, reauth, prtMethod);
                }
            }
            else
            {
                Logger.Error("Missing arguments for \"entra\" command");
                CommandLine.PrintUsage("entra");
            }
        }
    }
}
