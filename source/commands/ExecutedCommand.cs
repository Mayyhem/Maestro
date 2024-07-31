using System;
using System.Collections.Generic;
using System.IO;

namespace Maestro
{
    public class ExecutedCommand : Cloneable
    {
        public string Command { get; private set; }
        public Dictionary<string, string> Arguments { get; private set; }
        public Logger.LogLevel LogLevel { get; private set; } = Logger.LogLevel.Info;
        public LiteDBHandler Database { get; private set; }
        public bool Reauth { get; private set; }
        public bool DatabaseOnly { get; private set; }
        public int PrtMethod { get; private set; }

        public ExecutedCommand(Dictionary<string, string> parsedArguments)
        {
            Arguments = parsedArguments;

            if (Arguments.TryGetValue("command", out string command))
            {
                Command = command;
            }

            if (Arguments.TryGetValue("--database", out string databasePath))
            {
                Database =  new LiteDBHandler(databasePath);
                Logger.Info($"Using database file: {Path.GetFullPath(databasePath)}");
            }

            if (Arguments.TryGetValue("--prt-method", out string prtMethodString))
            {
                PrtMethod = int.Parse(prtMethodString);
            }

            if (Arguments.TryGetValue("--reauth", out string reauthString))
            {
                Reauth = bool.Parse(reauthString);
            }

            if (Arguments.TryGetValue("--show", out string databaseOnlyString))
            {
                DatabaseOnly = bool.Parse(databaseOnlyString);
            }

            if (Arguments.TryGetValue("--verbosity", out string logLevelString)
                && Enum.TryParse(logLevelString, true, out Logger.LogLevel logLevel))
            {
                LogLevel = logLevel;
            }
        }
    }
}
