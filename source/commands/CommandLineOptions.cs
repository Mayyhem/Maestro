using System;
using System.Collections.Generic;
using System.Linq;

namespace Maestro
{
    public class CommandLineOptions
    {
        // Global options
        public string Database { get; set; }
        public bool Help { get; set; }
        public int Verbosity { get; set; }

        // Command and subcommands
        public string Command { get; set; }
        public List<string> Subcommands { get; set; } = new List<string>();
        public string FullCommand { get; private set; }

        // Options
        public string AccessToken { get; set; }
        public string AppName { get; set; }
        public bool AsUser { get; set; }
        public string ClientId { get; set; }
        public bool Count { get; set; }
        public bool DryRun { get; set; }
        public string Extension { get; set; }
        public string Id { get; set; }
        public int Method { get; set; }
        public string Name { get; set; }
        public string OrderBy { get; set; }
        public string Path { get; set; }
        public string PrtCookie { get; set; }
        public int PrtMethod { get; set; }
        public List<string> Properties { get; set; }
        public string Query { get; set; }
        public bool Raw { get; set; }
        public bool Reauth { get; set; }
        public string RefreshToken { get; set; }
        public string Resource { get; set; }
        public int Retries { get; set; }
        public bool RunAsUser { get; set; }
        public string Scope { get; set; }
        public string Script { get; set; }
        public string TenantId { get; set; }
        public int Wait { get; set; }
        public string WhereCondition { get; set; }

        // Additional dictionary for any extra or custom options
        public Dictionary<string, string> AdditionalOptions { get; } = new Dictionary<string, string>();

        public CommandLineOptions() { }

        public static CommandLineOptions Parse(string[] args)
        {
            var options = new CommandLineOptions();
            var parsedArgs = CommandLine.ParseCommands(args);

            if (parsedArgs == null)
            {
                return null;
            }

            // Try to get the command and subcommands, accounting for null
            string thisCommandName = parsedArgs.TryGetValue("command", out string commandName) ? commandName : null;

            var command = CommandLine.commands.FirstOrDefault(c => c.Name == thisCommandName);
            if (command == null)
            {
                return null;
            }

            List<Option> allOptions = new List<Option>(CommandLine.GlobalOptions);
            allOptions.AddRange(command.Options);

            var subcommands = new List<Subcommand>();
            var currentSubcommands = command.Subcommands;
            for (int i = 1; parsedArgs.TryGetValue($"subcommand{i}", out string subcommandName); i++)
            {
                var subcommand = currentSubcommands.FirstOrDefault(s => s.Name == subcommandName);
                if (subcommand == null)
                {
                    break;
                }
                subcommands.Add(subcommand);
                allOptions.AddRange(subcommand.Options);
                currentSubcommands = subcommand.Subcommands;
            }

            // Populate properties based on parsed arguments and defaults
            foreach (var option in allOptions)
            {
                if (parsedArgs.TryGetValue(option.LongName, out string value))
                {
                    SetPropertyValue(options, option.LongName, value);
                }
                else if (!string.IsNullOrEmpty(option.Default))
                {
                    SetPropertyValue(options, option.LongName, option.Default);
                }
            }

            // Populate subcommands
            options.Command = commandName;
            options.Subcommands = subcommands.Select(s => s.Name).ToList();

            // Set FullCommand at the end of parsing
            options.FullCommand = string.Join(" ", new[] { options.Command }.Concat(options.Subcommands));

            return options;
        }

        private static void SetPropertyValue(CommandLineOptions options, string propertyName, string value)
        {
            propertyName = ToPascalCase(propertyName.TrimStart('-'));
            var property = typeof(CommandLineOptions).GetProperty(propertyName);
            if (property != null)
            {
                if (property.PropertyType == typeof(bool))
                {
                    property.SetValue(options, bool.Parse(value));
                }
                else if (property.PropertyType == typeof(int))
                {
                    property.SetValue(options, int.Parse(value));
                }
                else if (property.PropertyType == typeof(List<string>))
                {
                    property.SetValue(options, value.Split(',').ToList());
                }
                else
                {
                    property.SetValue(options, value);
                }
            }
            else
            {
                options.AdditionalOptions[propertyName] = value;
            }
        }

        private static string ToPascalCase(string s)
        {
            if (string.IsNullOrEmpty(s) || s.Length < 2)
                return s;

            string[] words = s.Split(new[] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
                }
            }
            return string.Join("", words);
        }
    }
}