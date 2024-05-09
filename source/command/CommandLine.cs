using System;
using System.Collections.Generic;

namespace Maestro
{
    internal static class CommandLine
    {
        public static Dictionary<string, string> Parse(string[] args)
        {
            if (args.Length == 0)
            {
                PrintGeneralUsage();
                return null;
            }

            string subCommand = args[0];
            switch (subCommand)
            {
                case "exec":
                    return ParseExecCommand(args);
                case "get":
                    return ParseGetCommand(args);
                default:
                    Console.WriteLine($"Invalid subcommand: {subCommand}");
                    PrintGeneralUsage();
                    return null;
            }
        }

        static Dictionary<string, string> ParseExecCommand(string[] args)
        {
            var parsedArguments = new Dictionary<string, string> { { "subcommand", "exec" } };

            if (args.Length == 1)
            {
                PrintExecUsage();
                return null;
            }

            for (int i = 1; i < args.Length; i += 2)
            {
                if (i + 1 >= args.Length)
                {
                    Console.WriteLine("Missing value for option.");
                    PrintExecUsage();
                    return null;
                }

                switch (args[i])
                {
                    case "-t":
                    case "--target":
                        parsedArguments["target"] = args[i + 1];
                        break;
                    case "-s":
                    case "--script":
                        parsedArguments["script"] = args[i + 1];
                        break;
                    default:
                        Console.WriteLine($"Invalid option: {args[i]}");
                        PrintExecUsage();
                        return null;
                }
            }
            return parsedArguments;
        }

        static Dictionary<string, string> ParseGetCommand(string[] args)
        {
            var parsedArguments = new Dictionary<string, string> { { "subcommand", "get" } };

            if (args.Length == 1)
            {
                PrintGetUsage();
                return null;
            }

            for (int i = 1; i < args.Length; i += 2)
            {
                if (i + 1 >= args.Length)
                {
                    Console.WriteLine("Missing value for option.");
                    PrintGetUsage();
                    return null;
                }

                if (args[i] == "-t" || args[i] == "--target")
                {
                    parsedArguments["target"] = args[i + 1];
                }
                else
                {
                    Console.WriteLine($"Invalid option: {args[i]}");
                    PrintGetUsage();
                    return null;
                }
            }
            return parsedArguments;
        }

        public static void PrintGeneralUsage()
        {
            Console.WriteLine("Usage: Maestro.exe <subcommand> [options]\n" +
                              "Subcommands:\n" +
                              "    exec - Execute a script on a target device.\n" +
                              "    get - Get information about a target device.\n");
        }

        public static void PrintExecUsage()
        {
            Console.WriteLine("Usage: exec -t <target> -s <script>\n" +
                              "    -t, --target <device>                    Target device to execute the script on\n" +
                              "    -s, --script <b64-encoded PS script>     Script to execute");
        }

        public static void PrintGetUsage()
        {
            Console.WriteLine("Usage: get -t <target>\n" +
                              "    -t, --target <device>                    Target device to get information from");
        }
    }
}
