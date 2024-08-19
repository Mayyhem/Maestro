using System.Threading.Tasks;

namespace Maestro
{
    public class LocalCommand
    {
        public static Task Execute(CommandLineOptions options, LiteDBHandler database)
        {
            if (options.Subcommands.Count == 0)
            {
                Logger.Error($"No subcommand specified for '{options.Command}' command.");
                CommandLine.PrintUsage($"{options.Command}");
                return null;
            }

            switch (options.Subcommands[0])
            {
                default:
                    Logger.Error($"Unknown subcommand for '{options.Command}': {options.Subcommands[0]}");
                    CommandLine.PrintUsage($"{options.Command}");
                    break;
            }
            return null;
        }
    }
}
