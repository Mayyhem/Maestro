using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maestro
{
    internal class IntuneCommand
    {
        public static async Task Execute(Dictionary<string, string> arguments, IDatabaseHandler database)
        {
            if (arguments.TryGetValue("subcommand", out string subcommandName))
            {
                if (subcommandName == "devices")
                {
                    await GetDevicesCommand.Execute(arguments, database);
                }
                else if (subcommandName == "devicequery")
                {
                    await DeviceQueryCommand.Execute(arguments, database);
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
                Logger.Error("Missing arguments for \"exec\" command");
                CommandLine.PrintUsage("exec");
            }
        }
    }
}
