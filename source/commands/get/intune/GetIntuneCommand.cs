using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maestro
{
    internal class GetIntuneCommand
    {
        public static async Task Execute(CommandLineOptions options, LiteDBHandler database)
        {
            int depth = 1;
            int parentDepth = depth - 1;

            if (options.Subcommands.Count == 0)
            {
                Logger.Error($"No subcommand specified for '{options.Subcommands[parentDepth]}' command.");
                CommandLine.PrintUsage($"{options.Subcommands[parentDepth]}");
                return;
            }

            switch (options.Subcommands[depth])
            {
                case "apps":
                    await GetIntuneAppsCommand.Execute(options, database);
                    break;
                case "devices":
                    break;
                case "refresh-token":
                    break;
                default:
                    Logger.Error($"Unknown subcommand for '{options.Subcommands[parentDepth]}': {options.Subcommands[depth]}");
                    CommandLine.PrintUsage($"{options.Subcommands[parentDepth]}");
                    break;
            }

            /*
            if (arguments.TryGetValue("subcommand1", out string subcommandName))
            {
                if (subcommandName == "apps")
                {
                }
                else if (subcommandName == "devices")
                {
                    await IntuneDevicesCommand.Execute(arguments, database, databaseOnly, reauth, prtMethod);
                }
                else if (subcommandName == "exec")
                {
                    await IntuneExecCommand.Execute(arguments, database, reauth);
                }
                else if (subcommandName == "scripts")
                {
                    await IntuneScriptsCommand.Execute(arguments, database, databaseOnly, reauth, prtMethod);
                }
                else if (subcommandName == "sync")
                {
                    await IntuneSyncCommand.Execute(arguments, database, reauth, prtMethod);
                }
            }
            else
            {
                Logger.Error("Missing arguments for \"intune\" command");
                CommandLine.PrintUsage("intune");
            }
            */
        }
    }
}
