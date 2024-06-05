using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maestro
{
    internal class EntraCmdHandler
    {
        public static async Task Execute(Dictionary<string, string> arguments, IDatabaseHandler database, bool databaseOnly)
        {
            if (arguments.TryGetValue("subcommand1", out string subcommandName))
            {
                if (subcommandName == "devices")
                {
                    //await DevicesCommand.Execute(arguments, database);
                }
                else if (subcommandName == "groups")
                {
                    await EntraGroupsCmdHandler.Execute(arguments, database);
                }
                else if (subcommandName == "users")
                {
                    //await IntuneSyncCommand.Execute(arguments, database);
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
