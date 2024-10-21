using System.Threading.Tasks;

namespace Maestro
{
    public class AddEntraCommand
    {
        public static async Task Execute(CommandLineOptions options, LiteDBHandler database)
        {
            // Remove the parent item from subcommands, then process the next subcommand
            options.Subcommands.RemoveAt(0);

            if (options.Subcommands.Count == 0)
            {
                Logger.Error($"No subcommand specified for '{options.FullCommand}' command.");
                CommandLine.PrintUsage($"{options.FullCommand}");
                return;
            }

            switch (options.Subcommands[0])
            {
                case "member":
                    await AddEntraMemberCommand.Execute(options, database);
                    break;
                default:
                    Logger.Error($"Unknown subcommand for '{options.FullCommand}");
                    CommandLine.PrintUsage($"{options.FullCommand}");
                    break;
            }
        }
    }
}