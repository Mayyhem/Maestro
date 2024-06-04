using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maestro
{
    internal class IntuneCommand
    {
        public static async Task Execute(Dictionary<string, string> arguments, IDatabaseHandler database)
        {
            if (arguments.TryGetValue("subcommand1", out string subcommandName))
            {
                if (subcommandName == "devices")
                {
                    await IntuneDevicesCommand.Execute(arguments, database);
                }
                else if (subcommandName == "exec")
                {
                    await IntuneExecCommand.Execute(arguments, database);
                }
                else if (subcommandName == "sync")
                {
                    await IntuneSyncCommand.Execute(arguments, database);
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
