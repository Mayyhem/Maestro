namespace Maestro
{
    public class ListCommand
    {
        public static void Execute(CommandLineOptions options)
        {
            if (options.Subcommands.Count == 0)
            {
                Logger.Error($"No subcommand specified for '{options.Command}' command.");
                CommandLine.PrintUsage(options.Command);
                return;
            }

            switch (options.Subcommands[0])
            {
                case "client-ids":
                    ListClientIdsCommand.Execute();
                    break;
                case "resource-ids":
                    ListResourceIdsCommand.Execute();
                    break;
                default:
                    Logger.Error($"Unknown subcommand for '{options.Command}': {options.Subcommands[0]}");
                    CommandLine.PrintUsage(options.Command);
                    break;
            }
        }
    }
}
