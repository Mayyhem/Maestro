using LiteDB;
using Maestro.source.commands.get;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Maestro
{
    class Program
    {
        private static async Task Main(string[] args)
        {
            // Execution timer
            var timer = new Stopwatch();

            // Database handler
            LiteDBHandler database = null;

            try
            {
                // Start timer
                timer.Start();

                // Parse arguments
                var options = CommandLineOptions.Parse(args);
                if (options == null || string.IsNullOrEmpty(options.Command)) 
                    return;

                Dictionary<string, string> parsedArguments = CommandLine.ParseCommands(args);
                if (parsedArguments == null || !parsedArguments.ContainsKey("command")) return;

                // Initialize the logger
                ILogger logger = new ConsoleLogger();
                Logger.LogLevel logLevel = (Logger.LogLevel)options.Verbosity;
                Logger.SetLogLevel(logger, logLevel);

                // Begin execution
                Logger.Info("Execution started");

                // Use database file if option is specified
                if (!string.IsNullOrEmpty(options.Database))
                {
                    database = LiteDBHandler.CreateOrOpen(options.Database);
                    if (database == null) return;
                    Logger.Info($"Using database file: {Path.GetFullPath(options.Database)}");
                }

                // Direct execution flow based on the command
                switch (options.Command)
                {
                    case "delete":
                        //await DeleteCommand.Execute(options, database);
                        break;
                    case "exec":
                        //await ExecCommand.Execute(options, database);
                        break;
                    case "get":
                        await GetCommand.Execute(options, database);
                        break;
                    case "list":
                        ListCommand.Execute(options);
                        break;
                    case "local":
                        break;
                    case "new":
                        //await NewCommand.Execute(options, database);
                        break;
                    case "show":
                        ShowCommand.Execute(options, database);
                        break;
                    case "store":
                        StoreCommand.Execute(options, database);
                        break;
                    default:
                        Logger.Error($"Unknown command: {parsedArguments["command"]}");
                        CommandLine.PrintUsage();
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.ExceptionDetails(ex);
            }

            finally
            {
                // Stop timer, release resources, and complete execution
                timer.Stop();
                database?.Dispose();
                Logger.Info($"Completed execution in {timer.Elapsed}");

                // Delay exit when debugging
                if (Debugger.IsAttached)
                    Console.ReadLine();
            }
        }
    }
}
