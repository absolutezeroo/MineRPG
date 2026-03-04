using System.Runtime.CompilerServices;

namespace MineRPG.Core.Logging;

/// <summary>
/// Writes formatted log lines to stdout/stderr.
/// Format: [LEVEL] HH:mm:ss.fff  message
/// Thread-safe via lock on a dedicated sync object.
/// </summary>
public sealed class ConsoleLogger : ILogger
{
    private readonly Lock _syncRoot = new();

    public LogLevel MinLevel { get; set; } = LogLevel.Debug;

    public void Debug(string message) => Write(LogLevel.Debug, message);
    public void Debug(string format, params object?[] args) => Write(LogLevel.Debug, Format(format, args));

    public void Info(string message) => Write(LogLevel.Info, message);
    public void Info(string format, params object?[] args) => Write(LogLevel.Info, Format(format, args));

    public void Warning(string message) => Write(LogLevel.Warning, message);
    public void Warning(string format, params object?[] args) => Write(LogLevel.Warning, Format(format, args));

    public void Error(string message) => Write(LogLevel.Error, message);
    public void Error(string message, Exception exception) => Write(LogLevel.Error, $"{message}\n{exception}");
    public void Error(string format, params object?[] args) => Write(LogLevel.Error, Format(format, args));
    public void Error(string format, Exception exception, params object?[] args)
        => Write(LogLevel.Error, $"{Format(format, args)}\n{exception}");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string Format(string format, object?[] args)
        => args.Length == 0 ? format : string.Format(format, args);

    private void Write(LogLevel level, string message)
    {
        if (level < MinLevel)
            return;

        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var label = level switch
        {
            LogLevel.Debug => "DEBUG  ",
            LogLevel.Info => "INFO   ",
            LogLevel.Warning => "WARNING",
            LogLevel.Error => "ERROR  ",
            _ => "UNKNOWN",
        };

        var line = $"[{label}] {timestamp}  {message}";

        lock (_syncRoot)
        {
            var target = level >= LogLevel.Error ? Console.Error : Console.Out;
            target.WriteLine(line);
        }
    }
}
