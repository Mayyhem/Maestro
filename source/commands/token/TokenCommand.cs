using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maestro
{ 
    internal class TokenCommand
    {
        public static async Task Execute(Dictionary<string, string> arguments, IDatabaseHandler database, bool databaseOnly, bool reauth)
        {
            if (arguments.TryGetValue("subcommand1", out string subcommandName))
            {
                if (subcommandName == "prt")
                {
                    await TokenPrtCommand.Execute(arguments, database, databaseOnly);
                }
                else if (subcommandName == "refresh")
                {
                    //await TokenRefreshCommand.Execute(arguments, database, databaseOnly, reauth);
                }
                else if (subcommandName == "access")
                {
                    await TokenAccessCommand.Execute(arguments, database, databaseOnly);
                }
            }
            else
            {
                Logger.Error("Missing arguments for \"token\" command");
                CommandLine.PrintUsage("token");
            }
        }
    }
}
