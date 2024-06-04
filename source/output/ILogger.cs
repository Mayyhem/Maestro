namespace Maestro
{
    public interface ILogger
    {
        Logger.LogLevel Level { get; }

        // Log with timestamps, levels, and messages
        void Log(Logger.LogLevel level, string message);

        // Write messages without timestamps or levels
        void WriteTextOnly(Logger.LogLevel level, string message);
    }
}
