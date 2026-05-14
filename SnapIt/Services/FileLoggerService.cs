using Serilog;
using SnapIt.Common.Contracts;

namespace SnapIt.Services;

public class FileLoggerService : ILoggerService
{
    private readonly ILogger logger;

    public FileLoggerService()
    {
        logger = Serilog.Log.Logger.ForContext<FileLoggerService>();
    }

    public void Log(LogLevel level, string message, string caller = "")
    {
        switch (level)
        {
            case LogLevel.Debug:
                logger.Debug("[{Caller}] {Message}", caller, message);
                break;
            case LogLevel.Info:
                logger.Information("[{Caller}] {Message}", caller, message);
                break;
            case LogLevel.Warn:
                logger.Warning("[{Caller}] {Message}", caller, message);
                break;
            case LogLevel.Error:
                logger.Error("[{Caller}] {Message}", caller, message);
                break;
        }
    }

    public void LogDebug(string message, string caller = "")
    {
        logger.Debug("[{Caller}] {Message}", caller, message);
    }

    public void LogInfo(string message, string caller = "")
    {
        logger.Information("[{Caller}] {Message}", caller, message);
    }

    public void LogWarn(string message, string caller = "")
    {
        logger.Warning("[{Caller}] {Message}", caller, message);
    }

    public void LogError(string message, string caller = "")
    {
        logger.Error("[{Caller}] {Message}", caller, message);
    }
}