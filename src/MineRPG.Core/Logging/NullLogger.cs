namespace MineRPG.Core.Logging;

/// <summary>
/// Silent logger for tests and contexts where logging is not needed.
/// All methods are no-ops.
/// </summary>
public sealed class NullLogger : ILogger
{
    public static readonly NullLogger Instance = new();

    public LogLevel MinLevel { get; set; } = LogLevel.Error;

    public void Debug(string message) { }
    public void Debug(string format, params object?[] args) { }
    public void Info(string message) { }
    public void Info(string format, params object?[] args) { }
    public void Warning(string message) { }
    public void Warning(string format, params object?[] args) { }
    public void Error(string message) { }
    public void Error(string message, Exception exception) { }
    public void Error(string format, params object?[] args) { }
    public void Error(string format, Exception exception, params object?[] args) { }
}
