namespace SnapIt.Common.Contracts;

public enum LogLevel
{
    Debug,
    Info,
    Warn,
    Error
}

public interface ILoggerService
{
    void Log(LogLevel level, string message, [System.Runtime.CompilerServices.CallerMemberName] string caller = "");

    void LogDebug(string message, [System.Runtime.CompilerServices.CallerMemberName] string caller = "");

    void LogInfo(string message, [System.Runtime.CompilerServices.CallerMemberName] string caller = "");

    void LogWarn(string message, [System.Runtime.CompilerServices.CallerMemberName] string caller = "");

    void LogError(string message, [System.Runtime.CompilerServices.CallerMemberName] string caller = "");
}