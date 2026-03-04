namespace MineRPG.Core.Logging;

/// <summary>
/// Centralized logger. Injected via constructor in pure projects.
/// In Godot bridge nodes, resolved via ServiceLocator.Get&lt;ILogger&gt;() in _Ready().
/// </summary>
public interface ILogger
{
    LogLevel MinLevel { get; set; }

    void Debug(string message);
    void Debug(string format, params object?[] args);

    void Info(string message);
    void Info(string format, params object?[] args);

    void Warning(string message);
    void Warning(string format, params object?[] args);

    void Error(string message);
    void Error(string message, Exception exception);
    void Error(string format, params object?[] args);
    void Error(string format, Exception exception, params object?[] args);
}
