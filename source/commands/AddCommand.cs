using System.Threading.Tasks;

namespace Maestro
{
    public class AddCommand
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
                case "entra":
                    await AddEntraCommand.Execute(options, database);
                    break;
                default:
                    Logger.Error($"Unknown subcommand for '{options.Command}': {options.Subcommands[0]}");
                    CommandLine.PrintUsage($"{options.Command}");
                    break;
            }
        }
    }
}