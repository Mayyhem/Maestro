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
            LiteDBHandler database = null;

            try
            {
                // Start timer and begin execution
                timer.Start();

                // Parse arguments
                Dictionary<string, string> parsedArguments = CommandLine.ParseCommands(args);
                if (parsedArguments == null || !parsedArguments.ContainsKey("command")) return;

                // Initialize the logger
                ILogger logger = new ConsoleLogger();
                if (parsedArguments.TryGetValue("--verbosity", out string logLevelString)
                    && Enum.TryParse(logLevelString, true, out Logger.LogLevel logLevel))
                {
                    // Set log level if specified
                    Logger.SetLogLevel(logger, logLevel);
                } 
                else
                {
                    // Log informational messages by default
                    Logger.SetLogLevel(logger, Logger.LogLevel.Info);
                }
                Logger.Info("Execution started");

                // Lookup credentials in database or force reauthentication
                bool reauth = false;

                // Use database file if option is specified
                if (parsedArguments.TryGetValue("--database", out string databasePath))
                {
                    database = LiteDBHandler.CreateOrOpen(databasePath);
                    if (database == null) return;

                    Logger.Info($"Using database file: {Path.GetFullPath(databasePath)}");

                    if (parsedArguments.TryGetValue("--reauth", out string reauthString))
                    {
                        reauth = bool.Parse(reauthString);
                    }
                }

                // Specify whether to only show database information (no API calls)
                bool databaseOnly = false;
                if (parsedArguments.TryGetValue("--show", out string databaseOnlyString))
                {
                    databaseOnly = bool.Parse(databaseOnlyString);
                }

                int prtMethod = 0;
                if (parsedArguments.TryGetValue("--prt-method", out string prtMethodString))
                {
                    prtMethod = int.Parse(prtMethodString);
                }

                // Direct execution flow based on the command
                switch (parsedArguments["command"])
                {
                    case "tokens":
                        await TokensCommand.Execute(parsedArguments, database, databaseOnly, reauth, prtMethod);
                        break;
                    case "entra":
                        await EntraCommand.Execute(parsedArguments, database, databaseOnly, reauth, prtMethod);
                        break;
                    case "intune":
                        await IntuneCommand.Execute(parsedArguments, database, databaseOnly, reauth, prtMethod);
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
