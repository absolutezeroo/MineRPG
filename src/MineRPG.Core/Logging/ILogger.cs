using System;

namespace MineRPG.Core.Logging;

/// <summary>
/// Centralized logger. Injected via constructor in pure projects.
/// In Godot bridge nodes, resolved via ServiceLocator.Get&lt;ILogger&gt;() in _Ready().
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Minimum log level. Messages below this level are discarded.
    /// </summary>
    LogLevel MinLevel { get; set; }

    /// <summary>Log a debug message.</summary>
    /// <param name="message">The message to log.</param>
    void Debug(string message);

    /// <summary>Log a formatted debug message.</summary>
    /// <param name="format">The format string with positional placeholders.</param>
    /// <param name="args">The format arguments.</param>
    void Debug(string format, params object?[] args);

    /// <summary>Log an informational message.</summary>
    /// <param name="message">The message to log.</param>
    void Info(string message);

    /// <summary>Log a formatted informational message.</summary>
    /// <param name="format">The format string with positional placeholders.</param>
    /// <param name="args">The format arguments.</param>
    void Info(string format, params object?[] args);

    /// <summary>Log a warning message.</summary>
    /// <param name="message">The message to log.</param>
    void Warning(string message);

    /// <summary>Log a formatted warning message.</summary>
    /// <param name="format">The format string with positional placeholders.</param>
    /// <param name="args">The format arguments.</param>
    void Warning(string format, params object?[] args);

    /// <summary>Log an error message.</summary>
    /// <param name="message">The message to log.</param>
    void Error(string message);

    /// <summary>Log an error message with an associated exception.</summary>
    /// <param name="message">The message to log.</param>
    /// <param name="exception">The exception that occurred.</param>
    void Error(string message, Exception exception);

    /// <summary>Log a formatted error message.</summary>
    /// <param name="format">The format string with positional placeholders.</param>
    /// <param name="args">The format arguments.</param>
    void Error(string format, params object?[] args);

    /// <summary>Log a formatted error message with an associated exception.</summary>
    /// <param name="format">The format string with positional placeholders.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="args">The format arguments.</param>
    void Error(string format, Exception exception, params object?[] args);
}
