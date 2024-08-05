using System;
using System.Collections.Generic;
using System.Linq;

namespace Maestro
{
    public class CommandLineOptions
    {
        // Global options
        public string DatabasePath { get; set; }
        public bool Help { get; set; }
        public int Verbosity { get; set; }

        // Command and subcommands
        public string Command { get; set; }
        public List<string> Subcommands { get; set; } = new List<string>();

        // Options
        public string AccessToken { get; set; }
        public string AppName { get; set; }
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
        public string Script { get; set; }
        public string TenantId { get; set; }
        public int Wait { get; set; }
        public string WhereCondition { get; set; }

        // Additional dictionary for any extra or custom options
        public Dictionary<string, string> AdditionalOptions { get; } = new Dictionary<string, string>();

        public static CommandLineOptions Parse(string[] args)
        {
            var options = new CommandLineOptions();
            var parsedArgs = CommandLine.ParseCommands(args);

            if (parsedArgs == null)
            {
                return null;
            }

            // Populate properties based on parsed arguments
            options.DatabasePath = GetOptionValue(parsedArgs, "--database");
            options.Help = parsedArgs.ContainsKey("--help");
            options.Verbosity = int.Parse(GetOptionValue(parsedArgs, "--verbosity", "2"));

            options.Command = GetOptionValue(parsedArgs, "command");

            // Populate subcommands
            for (int i = 1; i <= 5; i++) // Assuming max 5 levels of subcommands
            {
                string subcommand = GetOptionValue(parsedArgs, $"subcommand{i}");
                if (string.IsNullOrEmpty(subcommand)) break;
                options.Subcommands.Add(subcommand);
            }

            // Populate other properties
            options.AccessToken = GetOptionValue(parsedArgs, "--access-token");
            options.AppName = GetOptionValue(parsedArgs, "--name");
            options.Count = parsedArgs.ContainsKey("--count");
            options.DryRun = parsedArgs.ContainsKey("--dry-run");
            options.Extension = GetOptionValue(parsedArgs, "--extension", "Microsoft_AAD_IAM");
            options.Id = GetOptionValue(parsedArgs, "--id");
            options.Method = int.Parse(GetOptionValue(parsedArgs, "--method", "0"));
            options.Name = GetOptionValue(parsedArgs, "--name");
            options.OrderBy = GetOptionValue(parsedArgs, "--order-by");
            options.Path = GetOptionValue(parsedArgs, "--path");
            options.Properties = ParseList(GetOptionValue(parsedArgs, "--properties"));
            options.PrtCookie = GetOptionValue(parsedArgs, "--prt-cookie");
            options.PrtMethod = int.Parse(GetOptionValue(parsedArgs, "--prt-method", "0"));
            options.Query = GetOptionValue(parsedArgs, "--query");
            options.Raw = parsedArgs.ContainsKey("--raw");
            options.Reauth = parsedArgs.ContainsKey("--reauth");
            options.RefreshToken = GetOptionValue(parsedArgs, "--refresh-token");
            options.Resource = GetOptionValue(parsedArgs, "--resource", "microsoft.graph");
            options.Retries = int.Parse(GetOptionValue(parsedArgs, "--retries", "10"));
            options.RunAsUser = parsedArgs.ContainsKey("--user");
            options.Script = GetOptionValue(parsedArgs, "--script");
            options.TenantId = GetOptionValue(parsedArgs, "--tenant-id");
            options.Wait = int.Parse(GetOptionValue(parsedArgs, "--wait", "3"));
            options.WhereCondition = GetOptionValue(parsedArgs, "--where");

            // Store any additional options
            foreach (var kvp in parsedArgs)
            {
                if (!typeof(CommandLineOptions).GetProperty(ToPascalCase(kvp.Key))?.CanWrite ?? true)
                {
                    options.AdditionalOptions[kvp.Key] = kvp.Value;
                }
            }

            return options;
        }

        private static string GetOptionValue(Dictionary<string, string> parsedArgs, string key, string defaultValue = null)
        {
            return parsedArgs.TryGetValue(key, out string value) ? value : defaultValue;
        }

        private static List<string> ParseList(string value)
        {
            return value?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>();
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