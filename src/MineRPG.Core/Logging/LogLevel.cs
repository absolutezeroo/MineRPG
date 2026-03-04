namespace MineRPG.Core.Logging;

/// <summary>
/// Severity levels for the centralized logging system.
/// </summary>
public enum LogLevel
{
    /// <summary>Detailed diagnostic information for development only.</summary>
    Debug,

    /// <summary>Notable events during normal operation.</summary>
    Info,

    /// <summary>Recoverable issues that deserve attention.</summary>
    Warning,

    /// <summary>Failures that require investigation.</summary>
    Error,
}
