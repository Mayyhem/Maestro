namespace Maestro
{
    public interface ILogger
    {
        void Log(Logger.LogLevel level, string message);
    }
}