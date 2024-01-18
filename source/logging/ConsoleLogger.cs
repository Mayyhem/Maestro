using System;

namespace Maestro
{
    public class ConsoleLogger : ILogger
    {
        public void Log(Logger.LogLevel level, string message)
        {
            Console.WriteLine($"{DateTime.Now} - {level}: {message}");
        }
        
    }
}
