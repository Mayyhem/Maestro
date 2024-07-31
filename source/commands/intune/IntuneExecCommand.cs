using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maestro
{
    public class IntuneExecCommand : IntuneCommand
    {
        public IntuneExecCommand(Dictionary<string, string> arguments) : base(arguments) { }
        public async Task Execute()
        {
            if (Arguments.TryGetValue("subcommand2", out string subcommandName))
            {
                if (subcommandName == "app")
                {
                    //await IntuneExecAppCommand.Execute(arguments, database, reauth);
                    return;
                }
                else if (subcommandName == "query")
                {
                    //await IntuneExecQueryCommand.Execute(arguments, database, reauth);
                    return;
                }
                else if (subcommandName == "script")
                {
                    //await IntuneExecScriptCommand.Execute(arguments, database, reauth);
                    return;
                }
            }
            Logger.Error("Missing arguments for \"exec\" command");
            CommandLine.PrintUsage("exec");
        }
    }
}
