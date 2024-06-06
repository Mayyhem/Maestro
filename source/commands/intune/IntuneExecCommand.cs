using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maestro
{
    internal class IntuneExecCommand
    {
        public static async Task Execute(Dictionary<string, string> arguments, IDatabaseHandler database)
        {
            if (arguments.TryGetValue("subcommand2", out string subcommandName))
            {
                if (subcommandName == "query")
                {
                    await IntuneExecQueryCommand.Execute(arguments, database);
                    return;
                }
                else if (subcommandName == "script")
                {
                    await IntuneExecScriptCommand.Execute(arguments, database);
                    return;
                }
            }
            Logger.Error("Missing arguments for \"exec\" command");
            CommandLine.PrintUsage("exec");
        }
    }
}
