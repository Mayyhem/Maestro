using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maestro
{
    public class StoreCommand
    {
        public static void Execute(CommandLineOptions options, LiteDBHandler database)
        {
            if (options.Subcommands.Count == 0)
            {
                Logger.Error("No subcommand specified for 'store' command.");
                CommandLine.PrintUsage("store");
                return;
            }

            switch (options.Subcommands[0])
            {
                case "access-token":
                    StoreAccessTokenCommand.Execute(options, database);
                    break;
                case "prt-cookie":
                    break;
                case "refresh-token":
                    break;
                default:
                    Logger.Error($"Unknown subcommand for 'store': {options.Subcommands[0]}");
                    CommandLine.PrintUsage("store");
                    break;
            }
        }
    }
}
