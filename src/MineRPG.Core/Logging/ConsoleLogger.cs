using System;
using System.Runtime.CompilerServices;

namespace MineRPG.Core.Logging;

/// <summary>
/// Writes formatted log lines to stdout/stderr.
/// Format: [LEVEL] HH:mm:ss.fff  message
/// Thread-safe via lock on a dedicated sync object.
/// </summary>
public sealed class ConsoleLogger : ILogger
{
    private const string TimestampFormat = "HH:mm:ss.fff";
    private const string DebugLabel = "DEBUG  ";
    private const string InfoLabel = "INFO   ";
    private const string WarningLabel = "WARNING";
    private const string ErrorLabel = "ERROR  ";
    private const string UnknownLabel = "UNKNOWN";

    private readonly Lock _syncRoot = new();

    /// <summary>
    /// Minimum log level. Messages below this level are discarded.
    /// </summary>
    public LogLevel MinLevel { get; set; } = LogLevel.Debug;

    /// <inheritdoc />
    public void Debug(string message) => Write(LogLevel.Debug, message);

    /// <inheritdoc />
    public void Debug(string format, params object?[] args) => Write(LogLevel.Debug, Format(format, args));

    /// <inheritdoc />
    public void Info(string message) => Write(LogLevel.Info, message);

    /// <inheritdoc />
    public void Info(string format, params object?[] args) => Write(LogLevel.Info, Format(format, args));

    /// <inheritdoc />
    public void Warning(string message) => Write(LogLevel.Warning, message);

    /// <inheritdoc />
    public void Warning(string format, params object?[] args) => Write(LogLevel.Warning, Format(format, args));

    /// <inheritdoc />
    public void Error(string message) => Write(LogLevel.Error, message);

    /// <inheritdoc />
    public void Error(string message, Exception exception) => Write(LogLevel.Error, $"{message}\n{exception}");

    /// <inheritdoc />
    public void Error(string format, params object?[] args) => Write(LogLevel.Error, Format(format, args));

    /// <inheritdoc />
    public void Error(string format, Exception exception, params object?[] args)
        => Write(LogLevel.Error, $"{Format(format, args)}\n{exception}");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string Format(string format, object?[] args)
        => args.Length == 0 ? format : string.Format(format, args);

    private void Write(LogLevel level, string message)
    {
        if (level < MinLevel)
        {
            return;
        }

        string timestamp = DateTime.Now.ToString(TimestampFormat);
        string label = level switch
        {
            LogLevel.Debug => DebugLabel,
            LogLevel.Info => InfoLabel,
            LogLevel.Warning => WarningLabel,
            LogLevel.Error => ErrorLabel,
            _ => UnknownLabel,
        };

        string line = $"[{label}] {timestamp}  {message}";

        lock (_syncRoot)
        {
            TextWriter target = level >= LogLevel.Error ? Console.Error : Console.Out;
            target.WriteLine(line);
        }
    }
}
