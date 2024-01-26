using System;

namespace Maestro
{
    public static class Logger
    {
        public enum LogLevel
        {
            // Most to least verbose
            Debug,
            Verbose,
            Info,
            Warning,
            Error
        }
        private static LogLevel _logLevel;
        private static ILogger _logger;

        public static void Initialize(ILogger logger, LogLevel logLevel)
        {
            _logger = logger;
            _logLevel = logLevel;
        }

        public static void Log(LogLevel level, string message)
        {
            if (_logger == null || level < _logLevel) return;
            _logger?.Log(level, message);
        }

        public static void Debug(string message)
        {
            Log(LogLevel.Debug, message);
        }

        public static void Verbose(string message)
        {
            Log(LogLevel.Verbose, message);
        }

        public static void Info(string message)
        {
            Log(LogLevel.Info, message);
        }

        public static void Warning(string message)
        {
            Log(LogLevel.Warning, message);
        }

        public static void Error(string message)
        {
            Log(LogLevel.Error, message);
        }

        public static void ExceptionDetails(Exception ex)
        {
            Logger.Error($"An exception occurred!\n");
            while (ex != null)
            {
                Console.WriteLine($"  Exception type: {ex.GetType().Name}");
                Console.WriteLine($"  Message: {ex.Message}");
                Console.WriteLine($"  Stack Trace:\n {ex.StackTrace}");
                Console.WriteLine();
                ex = ex.InnerException;
            }
        }
        public static T NullError<T>(string message) where T : class
        {
            Logger.Error(message);
            return null;
        }
        public static T? NullErrorValue<T>(string message) where T : struct
        {
            Logger.Error(message);
            return null;
        }
    }
}
