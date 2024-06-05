using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maestro
{
    internal class IntuneCmdHandler
    {
        public static async Task Execute(Dictionary<string, string> arguments, IDatabaseHandler database, bool databaseOnly)
        {
            if (arguments.TryGetValue("subcommand1", out string subcommandName))
            {
                if (subcommandName == "devices")
                {
                    await IntuneDevicesCmdHandler.Execute(arguments, database, databaseOnly);
                }
                else if (subcommandName == "exec")
                {
                    await IntuneExecCmdHandler.Execute(arguments, database);
                }
                else if (subcommandName == "sync")
                {
                    await IntuneSyncCmdHandler.Execute(arguments, database);
                }
            }
            else
            {
                Logger.Error("Missing arguments for \"intune\" command");
                CommandLine.PrintUsage("intune");
            }
        }
    }
}
