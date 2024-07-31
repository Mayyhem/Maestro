using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maestro
{
    internal class EntraCommand
    {
        public static async Task Execute(Dictionary<string, string> arguments, LiteDBHandler database, bool databaseOnly, bool reauth, int prtMethod)
        {
            if (arguments.TryGetValue("subcommand1", out string subcommandName))
            {
                if (subcommandName == "devices")
                {
                    //await DevicesCommand.Execute(arguments, database);
                }
                else if (subcommandName == "groups")
                {
                    await EntraGroupsCommand.Execute(arguments, database, databaseOnly, reauth, prtMethod);
                }
                else if (subcommandName == "users")
                {
                    await EntraUsersCommand.Execute(arguments, database, databaseOnly, reauth, prtMethod);
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
