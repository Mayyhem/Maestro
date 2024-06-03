using System;
using System.Collections.Generic;
using System.Linq;

namespace Maestro
{
    internal static class CommandLine
    {
        private static List<Command> commands = new List<Command>
        {
            new Command
            {
                Name = "devicequery",
                Description = "Execute Intune device query on a target device",
                Options = new List<Option>
                {
                    new Option
                    {
                        ShortName = "-i",
                        LongName = "--id",
                        ValuePlaceholder = "ID",
                        Description = "ID of the device to get information for"
                    },
                    new Option
                    {
                        ShortName = "-n",
                        LongName = "--name",
                        ValuePlaceholder = "NAME",
                        Description = "Name of the device to get information for"
                    },
                    new Option
                    {
                        ShortName = "-q",
                        LongName = "--query",
                        ValuePlaceholder = "KQLQUERY",
                        Description = "The Kusto Query Language (KQL) query to execute on the device"
                    }
                }
            },
            new Command
            {
                Name = "exec",
                Description = "Execute a script on a target device",
                Options = new List<Option>
                {
                    new Option
                    {
                        ShortName = "-i",
                        LongName = "--id",
                        ValuePlaceholder = "ID",
                        Description = "ID of the device to execute the script on"
                    },
                    new Option
                    {
                        ShortName = "-s",
                        LongName = "--script",
                        ValuePlaceholder = "B64_SCRIPT",
                        Description = "Base64-encoded PowerShell script to execute"
                    }
                },
                Subcommands = new List<Subcommand>
                {
                    new Subcommand
                    {
                        Name = "sync",
                        Description = "Send notification to device requesting immediate sync to Intune",
                        Options = new List<Option>
                        {
                            new Option
                            {
                                ShortName = "-i",
                                LongName = "--id",
                                ValuePlaceholder = "ID",
                                Description = "ID of the device to sync"
                            }
                        }
                    }
                }
            },
            new Command
            {
                Name = "get",
                Description = "Get information from Azure services",
                Subcommands = new List<Subcommand>
                {
                    new Subcommand
                    {
                        Name = "devices",
                        Description = "Get information about Intune enrolled devices",
                        Options = new List<Option>
                        {
                            new Option
                            {
                                ShortName = "-i",
                                LongName = "--id",
                                ValuePlaceholder = "ID",
                                Description = "ID of the device to get information for"
                            },
                            new Option
                            {
                                ShortName = "-n",
                                LongName = "--name",
                                ValuePlaceholder = "NAME",
                                Description = "Name of the device to get information for"
                            },
                            new Option
                            {
                                ShortName = "-p",
                                LongName = "--properties",
                                ValuePlaceholder = "PROP,PROP | ALL",
                                Description = "Comma-separated list of properties to display or ALL to display all properties"
                            }
                        }
                    }
                }
            },
            new Command
            {
                Name = "show",
                Description = "Display information stored in the database",
                Subcommands = new List<Subcommand>
                {
                    new Subcommand
                    {
                        Name = "devices",
                        Description = "Show information about Intune enrolled devices",
                        Options = new List<Option>
                        {
                            new Option
                            {
                                ShortName = "-i",
                                LongName = "--id",
                                ValuePlaceholder = "ID",
                                Description = "ID of the device to show information for"
                            },
                            new Option
                            {
                                ShortName = "-n",
                                LongName = "--name",
                                ValuePlaceholder = "NAME",
                                Description = "Name of the device to show information for"
                            },
                            new Option
                            {
                                ShortName = "-p",
                                LongName = "--properties",
                                ValuePlaceholder = "PROP,PROP",
                                Description = "Comma-separated list of properties to display or ALL to display all properties"
                            }
                        }
                    }
                }
            }
        };

        private static readonly List<Option> GlobalOptions = new List<Option>
        {
            new Option
            {
                ShortName = "-d",
                LongName = "--database",
                ValuePlaceholder = "PATH.db",
                Description = "Database file used to read/write data that has already been queried"
            },
            new Option
            {
                ShortName = "-h",
                LongName = "--help",
                Description = "Display usage",
                IsFlag = true
            },
            new Option
            {
                ShortName = "-v",
                LongName = "--verbosity",
                ValuePlaceholder = "LEVEL",
                Description = @"Set the log verbosity level (default: 3)
                                1: Error
                                2: Warning
                                3: Info
                                4: Verbose
                                5: Debug"
            }
        };

        public static string PadDescription(string description)
        {
            return description.PadRight(30);
        }

        public static Dictionary<string, string> Parse(string[] args)
        {
            if (args.Length == 0)
            {
                Logger.Error("No arguments provided");
                PrintUsage();
                return null;
            }

            // Separate global options from the rest
            var parsedArguments = new Dictionary<string, string>();
            string[] remainingArgs = ParseGlobalOptions(args, parsedArguments);
            if (remainingArgs == null || remainingArgs.Length == 0)
            {
                Logger.Error("No arguments provided");
                PrintUsage();
                return parsedArguments;
            }

            string commandName = remainingArgs[0];
            var command = commands.FirstOrDefault(c => c.Name == commandName);

            if (command == null)
            {
                Logger.Error($"Invalid command: {commandName}");
                PrintUsage();
                return null;
            }

            parsedArguments["command"] = commandName;

            if (remainingArgs.Length > 1)
            {
                string potentialSubcommandOrOption = remainingArgs[1];
                var subcommand = command.Subcommands.FirstOrDefault(sc => sc.Name == potentialSubcommandOrOption);

                if (subcommand != null)
                {
                    parsedArguments["subcommand"] = potentialSubcommandOrOption;
                    var parsedSubcommandArgs = ParseOptions(remainingArgs.Skip(2).ToArray(), subcommand.Options, parsedArguments);
                    return parsedSubcommandArgs;
                }
                else
                {
                    var parsedCommandArgs = ParseOptions(remainingArgs.Skip(1).ToArray(), command.Options, parsedArguments);
                    return parsedCommandArgs;
                }
            }
            else
            {
                Logger.Error("No arguments provided");
                PrintUsage(commandName);
            }
            return null;
        }

        private static string[] ParseGlobalOptions(string[] args, Dictionary<string, string> parsedArguments)
        {
            var remainingArgsList = new List<string>();

            for (int i = 0; i < args.Length; i++)
            {
                var option = GlobalOptions.FirstOrDefault(o => o.ShortName == args[i] || o.LongName == args[i]);
                if (option != null)
                {
                    if (option.IsFlag)
                    {
                        parsedArguments[option.LongName] = "true";
                    }
                    else
                    {
                        if (i + 1 >= args.Length)
                        {
                            Logger.Error($"Missing value for global option: {args[i]}");
                            PrintUsage();
                            return null;
                        }
                        parsedArguments[option.LongName] = args[i + 1];
                        i++;
                    }
                }
                else
                {
                    remainingArgsList.Add(args[i]);
                }
            }

            return remainingArgsList.ToArray();
        }

        private static Dictionary<string, string> ParseOptions(string[] args, List<Option> options, Dictionary<string, string> parsedArguments)
        {
            for (int i = 0; i < args.Length; i++)
            {
                var option = options.FirstOrDefault(o => o.ShortName == args[i] || o.LongName == args[i]);
                if (option != null)
                {
                    if (option.IsFlag)
                    {
                        parsedArguments[option.LongName] = "true";
                    }
                    else
                    {
                        if (i + 1 >= args.Length)
                        {
                            Logger.Error($"Missing value for option: {args[i]}");
                            PrintUsage(parsedArguments["command"]);
                            return null;
                        }
                        parsedArguments[option.LongName] = args[i + 1];
                        i++;
                    }
                }
                else
                {
                    var globalOption = GlobalOptions.FirstOrDefault(o => o.ShortName == args[i] || o.LongName == args[i]);
                    if (globalOption != null)
                    {
                        if (globalOption.IsFlag)
                        {
                            parsedArguments[globalOption.LongName] = "true";
                        }
                        else
                        {
                            if (i + 1 >= args.Length)
                            {
                                Logger.Error($"Missing value for global option: {args[i]}");
                                PrintUsage();
                                return null;
                            }
                            parsedArguments[globalOption.LongName] = args[i + 1];
                            i++;
                        }
                    }
                    else
                    {
                        Logger.Error($"Invalid option: {args[i]}");
                        PrintUsage(parsedArguments["command"]);
                        return null;
                    }
                }
            }

            return parsedArguments;
        }

        public static void PrintUsage(string commandName = "")
        {
            Console.WriteLine();

            // Maestro.exe
            if (string.IsNullOrEmpty(commandName))
            {
                Console.WriteLine("Usage: Maestro.exe <command> [options]");
                Console.WriteLine("\nCommands:\n");
                foreach (var command in commands)
                {
                    Console.WriteLine(PadDescription($"  {command.Name}") + command.Description);
                    if (command.Subcommands.Any())
                    {
                        foreach (var subCommand in command.Subcommands)
                        {
                            Console.WriteLine(PadDescription($"    {subCommand.Name}") + subCommand.Description);
                        }
                    }
                    Console.WriteLine();
                }
                Console.WriteLine("Global Options:");
                foreach (var option in GlobalOptions)
                {
                    Console.WriteLine(PadDescription($"  {option.ShortName}, {option.LongName} {option.ValuePlaceholder}") + option.Description);
                }
            }

            else
            {
                // Maestro.exe <command>
                var command = commands.FirstOrDefault(c => c.Name == commandName);
                if (command != null)
                {
                    Console.WriteLine($"Usage: Maestro.exe {command.Name} [options]");
                    foreach (var option in command.Options)
                    {
                        Console.WriteLine(PadDescription($"    {option.ShortName}, {option.LongName} {option.ValuePlaceholder}") + option.Description);
                    }

                    if (command.Subcommands.Any())
                    {
                        Console.WriteLine($"\nSubcommands:");
                        foreach (var subCommand in command.Subcommands)
                        {
                            Console.WriteLine($"    {subCommand.Name} - {subCommand.Description}");
                            foreach (var option in subCommand.Options)
                            {
                                Console.WriteLine(PadDescription($"        {option.ShortName}, {option.LongName} {option.ValuePlaceholder}") + option.Description);
                            }
                        }
                    }
                }
                else
                {
                    // Maestro.exe <invalid-command>
                    Logger.Error($"Unknown command: {commandName}");
                }
            }
            Console.WriteLine();
        }
    }
}