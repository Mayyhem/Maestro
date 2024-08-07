namespace Maestro
{
    public class StoreCommand
    {
        public static void Execute(CommandLineOptions options, LiteDBHandler database)
        {
            if (options.Subcommands.Count == 0)
            {
                Logger.Error($"No subcommand specified for '{options.Command}' command.");
                CommandLine.PrintUsage(options.Command);
                return;
            }

            if (database == null)
            {
                Logger.Error("Please specify the path to a database file (-d)");
                CommandLine.PrintUsage(options.Command);
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
                    Logger.Error($"Unknown subcommand for '{options.Command}': {options.Subcommands[0]}");
                    CommandLine.PrintUsage(options.Command);
                    break;
            }
        }
    }
}
