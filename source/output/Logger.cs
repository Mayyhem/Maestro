using System;

namespace Maestro
{
    public static class Logger
    {
        public enum LogLevel
        {
            // Least to most verbose
            Error,
            Warning,
            Info,
            Verbose,
            Debug
        }
        private static LogLevel _logLevel;
        private static ILogger _logger;

        public static void SetLogLevel(ILogger logger, LogLevel logLevel)
        {
            _logger = logger;
            _logLevel = logLevel;
        }

        public static void Log(LogLevel logLevel, string message)
        {
            if (logLevel > _logLevel) return;
            _logger?.Log(logLevel, message);
        }

        public static void WriteTextOnly(LogLevel logLevel, string message)
        {
            if (logLevel > _logLevel) return;
            _logger?.WriteTextOnly(logLevel, message);
        }

        public static void Debug(string message)
        {
            Log(LogLevel.Debug, message);
        }

        public static void DebugTextOnly(string message)
        {
            WriteTextOnly(LogLevel.Debug, message);
        }

        public static void Verbose(string message)
        {
            Log(LogLevel.Verbose, message);
        }

        public static void VerboseTextOnly(string message)
        {
            WriteTextOnly(LogLevel.Verbose, message);
        }

        public static void Info(string message)
        {
            Log(LogLevel.Info, message);
        }

        public static void InfoTextOnly(string message)
        {
            WriteTextOnly(LogLevel.Info, message);
        }

        public static void Warning(string message)
        {
            Log(LogLevel.Warning, message);
        }

        public static void WarningTextOnly(string message)
        {
            WriteTextOnly(LogLevel.Warning, message);
        }

        public static void Error(string message)
        {
            Log(LogLevel.Error, message);
        }

        public static void ErrorTextOnly(string message)
        {
            WriteTextOnly(LogLevel.Error, message);
        }

        public static void ExceptionDetails(Exception ex)
        {
            Error($"An exception occurred!\n");
            while (ex != null)
            {
                Console.WriteLine($"  Exception type: {ex.GetType().Name}\n");
                Console.WriteLine($"  Message: {ex.Message}\n");
                Console.WriteLine($"  Stack Trace:\n {ex.StackTrace}\n");
                ex = ex.InnerException;
            }
        }
    }
}
