using System;
using System.Collections.Generic;
using System.Linq;

namespace Maestro
{
    internal static class CommandLine
    {
        private const int DescriptionPadding = 40;

        private static List<Command> commands = new List<Command>
        {
            new Command
            {
                Name = "intune",
                Description = "Execute actions in Intune and on Intune-enrolled devices",
                Subcommands = new List<Subcommand>
                {
                    new Subcommand
                    {
                        Name = "devices",
                        Description = "Get information about enrolled devices",
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
                    },
                    new Subcommand
                    {
                        Name = "sync",
                        Description = "Send notification to device requesting immediate sync to Intune",
                        Options = new List<Option>
                        {
                            new Option
                            {
                                Required = true,
                                ShortName = "-i",
                                LongName = "--id",
                                ValuePlaceholder = "ID",
                                Description = "ID of the device to sync"
                            }
                        }
                    },
                    new Subcommand
                    {
                        Name = "exec",
                        Description = "Execute actions on a device",
                        Subcommands = new List<Subcommand>
                        {
                            new Subcommand
                            {
                                Name = "query",
                                Description = "Execute device query on a device",
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
                                        Required = true,
                                        ShortName = "-q",
                                        LongName = "--query",
                                        ValuePlaceholder = "KQLQUERY",
                                        Description = "The Kusto Query Language (KQL) query to execute on the device"
                                    },
                                    new Option
                                    {
                                        ShortName = "-r",
                                        LongName = "--retries",
                                        ValuePlaceholder = "INT",
                                        Description = "Maximum number of attempts to fetch query results",
                                        Default = "10"
                                    },
                                    new Option
                                    {
                                        ShortName = "-w",
                                        LongName = "--wait",
                                        ValuePlaceholder = "INT",
                                        Description = "Number of seconds between each attempt to fetch query results",
                                        Default = "3"
                                    }
                                }
                            },
                            new Subcommand
                            {
                                Name = "script",
                                Description = "Execute a PowerShell script on a device",
                                Options = new List<Option>
                                {
                                    new Option
                                    {
                                        Required = true,
                                        ShortName = "-i",
                                        LongName = "--id",
                                        ValuePlaceholder = "ID",
                                        Description = "ID of the device to execute the script on"
                                    },
                                    new Option
                                    {
                                        Required = true,
                                        ShortName = "-s",
                                        LongName = "--script",
                                        ValuePlaceholder = "B64_SCRIPT",
                                        Description = "Base64-encoded PowerShell script to execute"
                                    }
                                },
                            },
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
                        Description = "Show information about Intune enrolled devices (default: all devices)",
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
                Description =
                    "Set the log verbosity level (default: 2)\n" +
                    new string(' ', DescriptionPadding) + "  0: Error\n" +
                    new string(' ', DescriptionPadding) + "  1: Warning\n" +
                    new string(' ', DescriptionPadding) + "  2: Info\n" +
                    new string(' ', DescriptionPadding) + "  3: Verbose\n" +
                    new string(' ', DescriptionPadding) + "  4: Debug",
                Default = "2"
            }
        };

        private static Subcommand FindSubcommand(List<Subcommand> subcommands, string subcommandName)
        {
            foreach (var subcommand in subcommands)
            {
                if (subcommand.Name == subcommandName)
                {
                    return subcommand;
                }

                var found = FindSubcommand(subcommand.Subcommands, subcommandName);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        public static string PadDescription(string description)
        {
            return description.PadRight(DescriptionPadding);
        }

        public static Dictionary<string, string> ParseCommands(string[] args, int depth = 0)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("No arguments provided");
                PrintUsage();
                return null;
            }

            // Separate global options from the rest
            var parsedArguments = new Dictionary<string, string>();
            string[] remainingArgs = ParseGlobalOptions(args, parsedArguments);
            if (remainingArgs == null || remainingArgs.Length == 0)
            {
                Console.WriteLine("No arguments provided");
                PrintUsage();
                return parsedArguments;
            }

            string commandName = remainingArgs[0];
            var command = commands.FirstOrDefault(c => c.Name == commandName);

            if (command == null)
            {
                Console.WriteLine($"Invalid command: {commandName}");
                PrintUsage();
                return null;
            }

            parsedArguments["command"] = commandName;

            if (remainingArgs.Length > 1)
            {
                return ParseSubcommands(command.Subcommands, remainingArgs.Skip(1).ToArray(), parsedArguments, depth + 1);
            }
            else
            {
                Console.WriteLine("No arguments provided");
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
                            Console.WriteLine($"Missing value for global option: {args[i]}");
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

            // Set default values for global options not provided by the user
            foreach (var option in GlobalOptions)
            {
                if (!parsedArguments.ContainsKey(option.LongName) && !string.IsNullOrEmpty(option.Default))
                {
                    parsedArguments[option.LongName] = option.Default;
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
                            Console.WriteLine($"Missing value for option: {args[i]}");
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
                                Console.WriteLine($"Missing value for global option: {args[i]}");
                                PrintUsage();
                                return null;
                            }
                            parsedArguments[globalOption.LongName] = args[i + 1];
                            i++;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Invalid option: {args[i]}");
                        PrintUsage(parsedArguments["command"]);
                        return null;
                    }
                }
            }

            // Set default values for options not provided by the user
            foreach (var option in options)
            {
                if (!parsedArguments.ContainsKey(option.LongName) && !string.IsNullOrEmpty(option.Default))
                {
                    parsedArguments[option.LongName] = option.Default;
                }
            }

            return parsedArguments;
        }

        private static Dictionary<string, string> ParseSubcommands(List<Subcommand> subcommands, string[] args, Dictionary<string, string> parsedArguments, int depth)
        {
            if (args.Length == 0)
            {
                return parsedArguments;
            }

            string subcommandName = args[0];
            var subcommand = subcommands.FirstOrDefault(sc => sc.Name == subcommandName);

            if (subcommand == null)
            {
                Console.WriteLine($"Invalid subcommand: {subcommandName}");
                PrintUsage(parsedArguments["command"]);
                return null;
            }

            parsedArguments[$"subcommand{depth}"] = subcommandName;

            if (args.Length > 1)
            {
                return ParseSubcommands(subcommand.Subcommands, args.Skip(1).ToArray(), parsedArguments, depth + 1);
            }
            else
            {
                var parsedSubcommandArgs = ParseOptions(args.Skip(1).ToArray(), subcommand.Options, parsedArguments);
                return parsedSubcommandArgs;
            }
        }

        private static void PrintCommandUsage(Command command, int depth)
        {
            Console.WriteLine($"Usage: Maestro.exe {command.Name} [options]");
            foreach (var option in command.Options)
            {
                PrintOptionUsage(option, depth);
            }

            if (command.Subcommands.Any())
            {
                foreach (var subCommand in command.Subcommands)
                {
                    Console.WriteLine();
                    PrintSubcommandUsage(subCommand, depth + 1);
                }
            }
        }

        private static void PrintSubcommandUsage(Subcommand subcommand, int depth)
        {
            Console.WriteLine(PadDescription($"{new string(' ', depth)}  {subcommand.Name}") + subcommand.Description);
            foreach (var option in subcommand.Options)
            {
                PrintOptionUsage(option, depth);
            }
            foreach (var subSubCommand in subcommand.Subcommands)
            {
                PrintSubcommandUsage(subSubCommand, depth + 2);
            }
        }

        private static void PrintOptionUsage(Option option, int depth)
        {
            string shortName = option.ShortName;
            string description = option.Description;
            if (option.Required)
            {
                description = $"(REQUIRED) {option.Description}";
            }
            if (!string.IsNullOrEmpty(option.Default))
            {
                description += $" (default: {option.Default})";
            }

            Console.WriteLine(PadDescription($"{new string(' ', depth)}    {shortName}, {option.LongName} {option.ValuePlaceholder}") + description);
        }

        public static void PrintUsage(string commandOrSubcommandName = "", int depth = 0)
        {
            Console.WriteLine();

            if (string.IsNullOrEmpty(commandOrSubcommandName))
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
                            PrintSubcommandUsage(subCommand, depth + 1);
                        }
                    }
                    Console.WriteLine();
                }
                Console.WriteLine("Global Options:\n");
                foreach (var option in GlobalOptions)
                {
                    Console.WriteLine(PadDescription($"  {option.ShortName}, {option.LongName} {option.ValuePlaceholder}") + option.Description);
                }
            }
            else
            {
                var command = commands.FirstOrDefault(c => c.Name == commandOrSubcommandName);
                if (command != null)
                {
                    PrintCommandUsage(command, depth);
                }
                else
                {
                    foreach (var cmd in commands)
                    {
                        var subcommand = FindSubcommand(cmd.Subcommands, commandOrSubcommandName);
                        if (subcommand != null)
                        {
                            PrintSubcommandUsage(subcommand, depth);
                            Console.WriteLine();
                            return;
                        }
                    }
                    // If not found
                    Console.WriteLine($"Unknown command or subcommand: {commandOrSubcommandName}");
                }
            }
            Console.WriteLine();
        }
    }
}
