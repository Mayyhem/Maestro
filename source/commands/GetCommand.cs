using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Maestro
{
    internal class GetCommand
    {
        public static async Task Execute(Dictionary<string, string> arguments, IDatabaseHandler database)
        {
            if (arguments.TryGetValue("subcommand", out string subcommandName))
            {
                if (subcommandName == "devices")
                {
                     await GetDevices.Execute(arguments, database);
                }
            }
            else
            {
                Logger.Error("Missing subcommand for \"get\" command");
                CommandLine.PrintUsage("get");
            }
        }
    }
}