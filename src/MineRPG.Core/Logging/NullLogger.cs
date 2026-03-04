using System;

namespace MineRPG.Core.Logging;

/// <summary>
/// Silent logger for tests and contexts where logging is not needed.
/// All methods are no-ops.
/// </summary>
public sealed class NullLogger : ILogger
{
    /// <summary>
    /// Shared singleton instance.
    /// </summary>
    public static readonly NullLogger Instance = new();

    /// <summary>
    /// Minimum log level. Defaults to Error since all methods are no-ops.
    /// </summary>
    public LogLevel MinLevel { get; set; } = LogLevel.Error;

    /// <inheritdoc />
    public void Debug(string message)
    {
    }

    /// <inheritdoc />
    public void Debug(string format, params object?[] args)
    {
    }

    /// <inheritdoc />
    public void Info(string message)
    {
    }

    /// <inheritdoc />
    public void Info(string format, params object?[] args)
    {
    }

    /// <inheritdoc />
    public void Warning(string message)
    {
    }

    /// <inheritdoc />
    public void Warning(string format, params object?[] args)
    {
    }

    /// <inheritdoc />
    public void Error(string message)
    {
    }

    /// <inheritdoc />
    public void Error(string message, Exception exception)
    {
    }

    /// <inheritdoc />
    public void Error(string format, params object?[] args)
    {
    }

    /// <inheritdoc />
    public void Error(string format, Exception exception, params object?[] args)
    {
    }
}
