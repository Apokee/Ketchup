namespace Ketchup.Services
{
    internal interface IDebugService
    {
        LogLevel Level { get; set; }

        void Log(string context, LogLevel level, string message, params object[] args);
    }
}
