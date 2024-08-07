using System;
using System.Collections.Generic;
using System.Linq;

namespace Maestro
{
    internal static class CommandLine
    {
        private const int DescriptionPadding = 40;

        public static readonly List<Option> GlobalOptions = new List<Option>
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
                LongName = "--raw",
                Description = "Do not pretty print results",
                IsFlag = true
            },
            new Option
            {
                ShortName = "-v",
                LongName = "--verbosity",
                ValuePlaceholder = "LEVEL",
                Description =
                    "Set the log verbosity level (default: 2)\n" +
                    new string(' ', DescriptionPadding) + "  0: Error (critical failures)\n" +
                    new string(' ', DescriptionPadding) + "  1: Warning (non-critical failures)\n" +
                    new string(' ', DescriptionPadding) + "  2: Info (need to know)\n" +
                    new string(' ', DescriptionPadding) + "  3: Verbose (useful)\n" +
                    new string(' ', DescriptionPadding) + "  4: Debug (detailed requests/responses)",
                Default = "2"
            }
        };

        public static List<Command> commands = new List<Command>
        {
            new Command
            {
                Name = "delete",
                Description = "Delete objects from Azure",
                Subcommands = new List<Subcommand>
                {
                }
            },
            new Command
            {
                Name = "exec",
                Description = "Execute actions on remote devices",
                Subcommands = new List<Subcommand>
                {
                    new Subcommand
                    {
                        Name = "intune",
                        Description = "Execute actions on devices enrolled in Intune",
                        Subcommands = new List<Subcommand>
                        {
                            new Subcommand
                            {
                                Name = "app",
                                Description = "Launch an executable from a specified UNC path on a device using a Win32 app",
                                Options = new List<Option>
                                {
                                    new Option
                                    {
                                        Required = true,
                                        ShortName = "-i",
                                        LongName = "--id",
                                        ValuePlaceholder = "ID",
                                        Description = "ID of the group to execute the app on"
                                    },
                                    new Option
                                    {
                                        Required = true,
                                        ShortName = "-n",
                                        LongName = "--name",
                                        ValuePlaceholder = "APPNAME",
                                        Description = "Name to give the application (visible in Intune)"
                                    },
                                    new Option
                                    {
                                        Required = true,
                                        ShortName = "-p",
                                        LongName = "--path",
                                        ValuePlaceholder = @"\\UNCPATH\APP.EXE",
                                        Description = "UNC path of the executable to launch"
                                    },
                                    new Option
                                    {
                                        LongName = "--as-user",
                                        Description = "Run as the currently logged in user (default: SYSTEM)",
                                        IsFlag = true
                                    }
                                }
                            },
                            new Subcommand
                            {
                                Name = "device-query",
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
                                        ShortName = "-n",
                                        LongName = "-name",
                                        ValuePlaceholder = "NAME",
                                        Description = "A name for the script"
                                    },
                                    new Option
                                    {
                                        Required = true,
                                        ShortName = "-s",
                                        LongName = "--script",
                                        ValuePlaceholder = "B64_SCRIPT",
                                        Description = "Base64-encoded PowerShell script to execute"
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
                            }
                        }
                    }
                },
            },
            new Command
            {
                Name = "get",
                Description = "Request information from Azure",
                Options = new List<Option>
                {
                    new Option
                    {
                        LongName = "--access-token",
                        ValuePlaceholder = "VALUE",
                        Description = "The access token to use (default: request an access token)"
                    },
                    new Option
                    {
                        ShortName = "-i",
                        LongName = "--id",
                        ValuePlaceholder = "ID",
                        Description = "ID of the object to get information for"
                    },
                    new Option
                    {
                        ShortName = "-n",
                        LongName = "--name",
                        ValuePlaceholder = "NAME",
                        Description = "Name of the object to get information for"
                    },
                    new Option
                    {
                        ShortName = "-p",
                        LongName = "--properties",
                        ValuePlaceholder = "PROP,PROP | ALL",
                        Description = "Comma-separated list of properties to get or ALL to get all properties"
                    },
                    new Option
                    {
                        LongName = "--prt-cookie",
                        ValuePlaceholder = "VALUE",
                        Description = "The PRT cookie to use (default: current user's PRT cookie)"
                    },
                    new Option
                    {
                        LongName = "--prt-method",
                        ValuePlaceholder = "METHOD",
                        Description =
                            "Method used to request PRT cookies from LSA Cloud AP plugin (default: 0)\n" +
                            new string(' ', DescriptionPadding) + "  0: RequestAADRefreshToken (via GetCookieInfoForUri COM interface)\n" +
                            new string(' ', DescriptionPadding) + "  1: ROADToken (spawn BrowserCore.exe and call GetCookies)",
                        Default = "0"
                    },
                    new Option
                    {
                        LongName = "--reauth",
                        Description = "Skip database credential lookup and force reauthentication",
                        IsFlag = true
                    },
                    new Option
                    {
                        LongName = "--refresh-token",
                        ValuePlaceholder = "VALUE",
                        Description = "The refresh token to use (default: request a refresh token)"
                    },
                    /*
                    new Option
                    {
                        ShortName = "-q",
                        LongName = "--query",
                        ValuePlaceholder = "ODATA",
                        Description = "Append the MSGraph request URL with arbitrary OData system query options"
                    },
                    */
                    new Option
                    {
                        ShortName = "-t",
                        LongName = "--tenant-id",
                        ValuePlaceholder = "ID",
                        Description = "The tenant ID to use (default: obtain from /signin)"
                    }
                },
                Subcommands = new List<Subcommand>
                {
                    new Subcommand
                    {
                        Name = "access-token",
                        Description = "Get an access token",
                        Options = new List<Option>
                        {
                            new Option
                            {
                                ShortName = "-c",
                                LongName = "--client-id",
                                ValuePlaceholder = "VALUE",
                                Description = "The client ID / app ID to request an access token for (default: Azure Portal)",
                                Default = "c44b4083-3bb0-49c1-b47d-974e53cbdf3c"
                            },
                            new Option
                            {
                                ShortName = "-e",
                                LongName = "--extension",
                                ValuePlaceholder = "NAME",
                                Description = "Name of the extension to request an access token for",
                                Default = "Microsoft_AAD_IAM"
                            },
                            new Option
                            {
                                ShortName = "-m",
                                LongName = "--method",
                                ValuePlaceholder = "METHOD",
                                Description =
                                    "Method used to request access tokens (default: 0)\n" +
                                    new string(' ', DescriptionPadding) + "  0: /oauth2/v2.0/token\n" +
                                    new string(' ', DescriptionPadding) + "  1: /api/DelegationToken",
                                Default = "0"
                            },
                            new Option
                            {
                                ShortName = "-r",
                                LongName = "--resource",
                                ValuePlaceholder = "RESOURCE",
                                Description = "Name or ID of the resource to request an access token for (default: MSGraph)",
                                Default = "https://graph.microsoft.com"
                            },
                            new Option
                            {
                                ShortName = "-s",
                                LongName = "--scope",
                                ValuePlaceholder = "SCOPE",
                                Description = "The scope to request",
                                Default = ".default"
                            },
                        }
                    },
                    new Subcommand
                    {
                        Name = "entra",
                        Description = "Get information about EntraID",
                        Subcommands = new List<Subcommand>
                        {
                            new Subcommand
                            {
                                Name = "groups",
                                Description = "Get information about EntraID groups"

                            },
                            new Subcommand
                            {
                                Name = "users",
                                Description = "Get information about EntraID users"
                            }
                        }
                    },
                    new Subcommand
                    {
                        Name = "intune",
                        Description = "Get information about Intune and Intune-enrolled devices",
                        Subcommands = new List<Subcommand>
                        {
                            new Subcommand
                            {
                                Name = "apps",
                                Description = "Get information about Intune apps"
                            },
                            new Subcommand
                            {
                                Name = "devices",
                                Description = "Get information about enrolled devices"
                            },
                            new Subcommand
                            {
                                Name = "scripts",
                                Description = "Get information about scripts and remediations"
                            }
                        }
                    },
                    new Subcommand
                    {
                        Name = "prt-cookie",
                        Description = "Get a nonce and PRT cookie"
                    },
                    new Subcommand
                    {
                        Name = "refresh-token",
                        Description = "Get a refresh token"
                    },
                }
            },
            new Command
            {
                Name = "list",
                Description = "List common object values",
                Subcommands = new List<Subcommand>
                {
                    new Subcommand
                    {
                        Name = "client-ids",
                        Description = "List common client IDs"
                    },
                    new Subcommand
                    {
                        Name = "resource-ids",
                        Description = "List resource IDs"
                    },
                }
            },
            new Command
            {
                Name = "local",
                Description = "Execute actions on the local device",
                Subcommands = new List<Subcommand>
                {
                }
            },
            new Command
            {
                Name = "new",
                Description = "Create new objects in Azure",
                Subcommands = new List<Subcommand>
                {
                }
            },

            new Command
            {
                Name = "show",
                Description = "Display information stored in the database",
                Options = new List<Option>
                {
                    new Option
                    {
                        ShortName = "-c",
                        LongName = "--count",
                        Description = "Count results only",
                        IsFlag = true
                    },
                    new Option
                    {
                        ShortName = "-o",
                        LongName = "--order-by",
                        ValuePlaceholder = "PROP (ASC|DESC)",
                        Description = "An ORDER BY clause to set the order of data returned by the query"
                    },
                    new Option
                    {
                        ShortName = "-p",
                        LongName = "--properties",
                        ValuePlaceholder = "PROP,PROP",
                        Description = "Comma-separated list of properties to get or ALL to get all properties"
                    },
                    new Option
                    {

                        ShortName = "-w",
                        LongName = "--where",
                        ValuePlaceholder = "CONDITION",
                        Description = "WHERE condition(s) to narrow the scope of data returned by the query"
                    },
                    new Option
                    {
                        ShortName = "-z",
                        LongName = "--dry-run",
                        Description = "Display the resulting query but do not run it",
                        IsFlag = true
                    },
                },
                Subcommands = new List<Subcommand>
                {
                    new Subcommand
                    {
                        Name = "access-tokens",
                        Description = "Show stored access tokens"
                    },
                    new Subcommand
                    {
                        Name = "devices",
                        Description = "Show information about Intune enrolled devices (default: all devices)"
                    },
                }
            },
            new Command
            {
                Name = "store",
                Description = "Store information in the database",
                Subcommands = new List<Subcommand>
                {
                    new Subcommand
                    {
                        Name = "access-token",
                        Description = "Store an access token",
                        Options = new List<Option>
                        {
                            new Option
                            {
                                Required = true,
                                ShortName = "-t",
                                LongName = "--access-token",
                                ValuePlaceholder = "TOKEN",
                                Description = "Access token to store"
                            }
                        }
                    },
                    new Subcommand
                    {
                        Name = "prt-cookie",
                        Description = "Store a PRT cookie",
                        Options = new List<Option>
                        {
                            new Option
                            {
                                Required = true,
                                ShortName = "-c",
                                LongName = "--prt-cookie",
                                ValuePlaceholder = "COOKIE",
                                Description = "PRT cookie to store"
                            }
                        }
                    },
                    new Subcommand
                    {
                        Name = "refresh-token",
                        Description = "Store a refresh token",
                        Options = new List<Option>
                        {
                            new Option
                            {
                                Required = true,
                                ShortName = "-t",
                                LongName = "--refresh-token",
                                ValuePlaceholder = "TOKEN",
                                Description = "Refresh token to store"
                            }
                        }
                    }
                }
            }
        };

        // Check whether a subcommand exists in a list of subcommands
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

        // Find a subcommand by its full path
        private static Subcommand FindSubcommandByPath(List<Command> commands, string[] path)
        {
            if (path.Length == 0)
                return null;

            var command = commands.FirstOrDefault(c => c.Name == path[0]);
            if (command == null)
                return null;

            var currentSubcommands = command.Subcommands;
            for (int i = 1; i < path.Length; i++)
            {
                var subcommand = currentSubcommands.FirstOrDefault(sc => sc.Name == path[i]);
                if (subcommand == null)
                    return null;

                if (i == path.Length - 1)
                    return subcommand;

                currentSubcommands = subcommand.Subcommands;
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
                Console.WriteLine("\nNo arguments provided");
                PrintUsage();
                return null;
            }

            // Separate global options from the rest
            var parsedArguments = new Dictionary<string, string>();
            string[] remainingArgs = ParseGlobalOptions(args, parsedArguments);

            if (remainingArgs == null || remainingArgs.Length == 0)
            {
                PrintUsage();
                return parsedArguments;
            }

            if (parsedArguments.ContainsKey("--help"))
            {
                PrintUsage();
                return null;
            }

            string commandName = remainingArgs[0];
            var command = commands.FirstOrDefault(c => c.Name == commandName);

            if (command == null)
            {
                Console.WriteLine($"\nInvalid command: {commandName}");
                PrintUsage();
                return null;
            }

            parsedArguments["command"] = commandName;

            if (remainingArgs.Length > 1)
            {
                var result = ParseSubcommands(command.Subcommands, remainingArgs.Skip(1).ToArray(), parsedArguments, depth + 1, command);

                // Invalid command or subcommand
                if (result == null)
                {
                    PrintCommandUsage(command, depth);
                    Console.WriteLine();
                    return null;
                }
                return result;
            }

            else
            {
                if (parsedArguments.ContainsKey("--help"))
                {
                    PrintCommandUsage(command, depth);
                    return null;
                }
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
                        if (i + 1 >= args.Length || args[i + 1].StartsWith("-"))
                        {
                            Console.WriteLine($"\nMissing value for global option: {args[i]}");
                            PrintOptionUsage(option, 0);
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
                // Subcommand options
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
                            Console.WriteLine($"\nMissing value for option: {args[i]}\n");
                            PrintOptionUsage(option, 0);
                            Console.WriteLine();
                            return null;
                        }
                        parsedArguments[option.LongName] = args[i + 1];
                        i++;
                    }
                }
                
                else
                {
                    // Global options
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
                                Console.WriteLine($"\nMissing value for global option: {args[i]}");
                                PrintOptionUsage(globalOption, 0);
                                return null;
                            }
                            parsedArguments[globalOption.LongName] = args[i + 1];
                            i++;
                        }
                    }

                    // Invalid options
                    else
                    {
                        if (args[i].StartsWith("-"))
                        {
                            Console.WriteLine($"\nInvalid option: {args[i]}");
                        }
                        else
                        {
                            Console.WriteLine($"\nInvalid subcommand: {args[i]}");
                        }
                        Console.WriteLine();
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

        private static Dictionary<string, string> ParseSubcommands(List<Subcommand> subcommands, string[] args, Dictionary<string, string> parsedArguments, int depth, Command parentCommand = null)
        {
            if (args.Length == 0)
            {
                return parsedArguments;
            }

            string subcommandOrOptionNameArg = args[0];

            var subcommandOrOption = subcommands.FirstOrDefault(sc => sc.Name == subcommandOrOptionNameArg);

            if (subcommandOrOption != null)
            {
                parsedArguments[$"subcommand{depth}"] = subcommandOrOption.Name;

                // Combine parent options with current subcommand options
                var combinedOptions = new List<Option>();
                if (parentCommand != null)
                {
                    combinedOptions.AddRange(parentCommand.Options);
                }
                combinedOptions.AddRange(subcommandOrOption.Options);

                if (args.Length > 1)
                {
                    // Check for nested subcommands
                    if (subcommandOrOption.Subcommands.Any())
                    {
                        var matchedSubcommand = FindSubcommand(subcommandOrOption.Subcommands, args[1]);
                        if (matchedSubcommand != null)
                        {
                            return ParseSubcommands(subcommandOrOption.Subcommands, args.Skip(1).ToArray(), parsedArguments, depth + 1, parentCommand);
                        }
                    }
                }

                // Parse options including inherited parent options
                return ParseOptions(args.Skip(1).ToArray(), combinedOptions, parsedArguments);
            }

            // Check for invalid subcommands
            if (parentCommand != null && !parentCommand.Options.Any(o => o.LongName == subcommandOrOptionNameArg))
            {
                Console.WriteLine($"\nInvalid subcommand: {subcommandOrOptionNameArg}\n");
                return null;
            }

            // Parse options including inherited parent options
            if (parentCommand != null && parentCommand.Options.Count > 0)
            {
                return ParseOptions(args.Skip(1).ToArray(), parentCommand.Options, parsedArguments);
            }

            return parsedArguments;
        }

        private static void PrintCommandUsage(Command command, int depth)
        {
            Console.WriteLine($"Usage: Maestro.exe {command.Name} [subcommand] [options]");
            Console.WriteLine("\nGlobal Options:\n");
            foreach (var option in GlobalOptions)
            {
                string shortNameOrNot = $"  {(!string.IsNullOrEmpty(option.ShortName) ? option.ShortName + "," : "   ")}";
                Console.WriteLine(PadDescription($"{shortNameOrNot}{option.LongName} {option.ValuePlaceholder}") + option.Description);
            }

            if (command.Options.Count > 0)
            {
                Console.WriteLine("\n   Options:");
                foreach (var option in command.Options)
                {
                    PrintOptionUsage(option, depth);
                }
            }

            if (command.Subcommands.Any())
            {
                Console.WriteLine("\n   Subcommands:");
                foreach (var subCommand in command.Subcommands)
                {
                    Console.WriteLine();
                    PrintSubcommandUsage(subCommand, depth + 1);
                }
            }
        }

        private static void PrintSubcommandUsage(Subcommand subcommand, int depth)
        {
            Console.WriteLine(PadDescription($"{new string(' ', depth)}    {subcommand.Name}") + subcommand.Description);
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
            string description = option.Description;
            if (option.Required)
            {
                description = $"(REQUIRED) {option.Description}";
            }
            if (!string.IsNullOrEmpty(option.Default))
            {
                description += $" (default: {option.Default})";
            }
            string shortNameOrNot = $"  {(!string.IsNullOrEmpty(option.ShortName) ? option.ShortName + "," : "   ")}";
            Console.WriteLine(PadDescription($"{new string(' ', depth)}    {shortNameOrNot}{option.LongName} {option.ValuePlaceholder}") + description);
        }

        public static void PrintUsage(string commandPath = "", int depth = 0)
        {
            Console.WriteLine();

            if (string.IsNullOrEmpty(commandPath))
            {
                Console.WriteLine("Usage: Maestro.exe <command> [subcommand] [options]");
                Console.WriteLine("\nGlobal Options:\n");
                foreach (var option in GlobalOptions)
                {
                    string shortNameOrNot = $"  {(!string.IsNullOrEmpty(option.ShortName) ? option.ShortName + "," : "   ")}";
                    Console.WriteLine(PadDescription($"{shortNameOrNot}{option.LongName} {option.ValuePlaceholder}") + option.Description);
                }

                Console.WriteLine("\nCommands:\n");
                foreach (var command in commands)
                {
                    Console.WriteLine(PadDescription($"   {command.Name}") + command.Description);
                    if (command.Options.Count > 0)
                    {
                        //Console.WriteLine("\n   Options:");
                        foreach (var option in command.Options)
                        {
                            string shortNameOrNot = $"     {(!string.IsNullOrEmpty(option.ShortName) ? option.ShortName + "," : "   ")}";
                            Console.WriteLine(PadDescription($"{shortNameOrNot}{option.LongName} {option.ValuePlaceholder}") + option.Description);
                        }
                    }
                    Console.WriteLine();
                    if (command.Subcommands.Any())
                    {
                        //Console.WriteLine("\n   Subcommands:");
                        foreach (var subCommand in command.Subcommands)
                        {
                            PrintSubcommandUsage(subCommand, depth + 1);
                        }
                        Console.WriteLine();
                    }
                    if (command != commands.Last())
                    {
                        Console.WriteLine();
                    }
                }
            }
            else
            {
                string[] pathParts = commandPath.Split(' ');
                var command = commands.FirstOrDefault(c => c.Name == pathParts[0]);
                if (command != null)
                {
                    if (pathParts.Length == 1)
                    {
                        PrintCommandUsage(command, depth);
                    }
                    else
                    {
                        var subcommand = FindSubcommandByPath(commands, pathParts);
                        if (subcommand != null)
                        {
                            Console.WriteLine($"Usage: Maestro.exe {commandPath} [options]");
                            Console.WriteLine($"\nDescription: {subcommand.Description}\n");
                            if (subcommand.Options.Any())
                            {
                                Console.WriteLine("Options:");
                                foreach (var option in subcommand.Options)
                                {
                                    PrintOptionUsage(option, depth);
                                }
                            }
                            if (subcommand.Subcommands.Any())
                            {
                                Console.WriteLine("\nSubcommands:");
                                foreach (var subSubcommand in subcommand.Subcommands)
                                {
                                    PrintSubcommandUsage(subSubcommand, depth + 1);
                                }
                            }
                            Console.WriteLine("\nGlobal Options:");
                            foreach (var option in GlobalOptions)
                            {
                                string shortNameOrNot = $"  {(!string.IsNullOrEmpty(option.ShortName) ? option.ShortName + "," : "   ")}";
                                Console.WriteLine(PadDescription($"{shortNameOrNot}{option.LongName} {option.ValuePlaceholder}") + option.Description);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Unknown command or subcommand: {commandPath}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Unknown command: {pathParts[0]}");
                }
            }
            Console.WriteLine();
        }
    }
}
