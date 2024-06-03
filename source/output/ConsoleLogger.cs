using System;
using System.Globalization;

namespace Maestro
{
    public class ConsoleLogger : ILogger
    {
        public Logger.LogLevel Level { get; }

        public void Log(Logger.LogLevel level, string message)
        {
            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            Console.WriteLine($"{timestamp} UTC - [{level.ToString().ToUpper()}]".PadRight(38) + message);
        }
    }
}
