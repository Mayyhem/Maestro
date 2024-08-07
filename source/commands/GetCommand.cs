using System.Threading.Tasks;

namespace Maestro
{
    public class GetCommand
    {
        public static async Task Execute(CommandLineOptions options, LiteDBHandler database)
        {
            if (options.Subcommands.Count == 0)
            {
                Logger.Error($"No subcommand specified for '{options.Command}' command.");
                CommandLine.PrintUsage($"{options.Command}");
                return;
            }

            switch (options.Subcommands[0])
            {
                case "access-token":
                    await GetAccessTokenCommand.Execute(options, database);
                    break;
                case "entra":
                    await GetEntraCommand.Execute(options, database);
                    break;
                case "intune":
                    await GetIntuneCommand.Execute(options, database);
                    break;
                case "prt-cookie":
                    break;
                case "refresh-token":
                    break;
                default:
                    Logger.Error($"Unknown subcommand for '{options.Command}': {options.Subcommands[0]}");
                    CommandLine.PrintUsage($"{options.Command}");
                    break;
            }
        }
    }
}
