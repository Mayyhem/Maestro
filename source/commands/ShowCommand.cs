using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maestro
{
    internal class ShowCommand
    {
        public static async Task Execute(Dictionary<string, string> arguments, IDatabaseHandler database)
        {
            if (arguments.TryGetValue("subcommand", out string subcommandName))
            {
                if (subcommandName == "devices")
                {
                    await GetDevices.Execute(arguments, database, databaseOnly: true);
                }
            }
            else
            {
                Logger.Error("Missing subcommand for \"show\" command");
                CommandLine.PrintUsage("show");
            }
        }
    }
}