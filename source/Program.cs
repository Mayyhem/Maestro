using Maestro.source.command;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Maestro
{
    class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                // Parse arguments
                Dictionary<string, string> parsedArguments = CommandLine.Parse(args);
                if (parsedArguments == null)
                {
                    Console.WriteLine("Failed to parse command line arguments or no arguments provided.");
                    return;
                }

                // Logging and debugging
                Logger.LogLevel logLevel = Logger.LogLevel.Info;
                if (args.Contains<string>("--debug"))
                {
                    logLevel = Logger.LogLevel.Debug;
                }
                ILogger logger = new ConsoleLogger();
                Logger.Initialize(logger, logLevel);

                // Execution timer
                var timer = new Stopwatch();
                timer.Start();
                Logger.Info("Execution started");

                // Directing execution flow based on the subcommand
                switch (parsedArguments["subcommand"])
                {
                    case "exec":
                        ExecCommand.Execute(parsedArguments);
                        break;
                    case "get":
                        GetCommand.Execute(parsedArguments);
                        break;
                    default:
                        Console.WriteLine("Unknown subcommand. Please check the usage.");
                        CommandLine.PrintGeneralUsage();
                        break;
                }

                // Stop timer and complete execution
                timer.Stop();
                Logger.Info($"Completed execution in {timer.Elapsed}");

            }
            catch (Exception ex)
            {
                Logger.ExceptionDetails(ex);
            }

            // Delay completion when debugging
            if (Debugger.IsAttached)
                Console.ReadLine();
        }
    }
}
