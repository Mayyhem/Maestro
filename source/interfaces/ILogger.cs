namespace Maestro
{
    public interface ILogger
    {
        Logger.LogLevel Level { get; }
        void Log(Logger.LogLevel level, string message);
    }
}