using Maestro.source.command;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Maestro
{
    class Program
    {
        private static void Main(string[] args)
        {
            // Execution timer
            var timer = new Stopwatch();

            try
            {
                // Logging and debugging
                Logger.LogLevel logLevel = Logger.LogLevel.Info;
                if (args.Contains<string>("--debug"))
                {
                    logLevel = Logger.LogLevel.Debug;
                }
                ILogger logger = new ConsoleLogger();
                Logger.Initialize(logger, logLevel);

                // Start timer and begin execution
                timer.Start();
                Logger.Info("Execution started");

                // Parse arguments
                Dictionary<string, string> parsedArguments = CommandLine.Parse(args);
                if (parsedArguments == null) return;

                // Directing execution flow based on the command
                switch (parsedArguments["command"])
                {
                    case "exec":
                        ExecCommand.Execute(parsedArguments);
                        break;
                    case "get":
                        GetCommand.Execute(parsedArguments);
                        break;
                    default:
                        Console.WriteLine("Unknown command");
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
                // Stop timer and complete execution
                timer.Stop();
                Logger.Info($"Completed execution in {timer.Elapsed}");

                // Delay exit when debugging
                if (Debugger.IsAttached)
                    Console.ReadLine();
            }
        }
    }
}
