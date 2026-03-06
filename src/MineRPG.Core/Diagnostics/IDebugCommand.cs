namespace MineRPG.Core.Diagnostics;

/// <summary>
/// A debug command that can be executed from the debug console.
/// Implementations are registered in <see cref="DebugCommandRegistry"/>.
/// </summary>
public interface IDebugCommand
{
    /// <summary>
    /// The primary command name (e.g., "tp", "render_distance").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Short description shown in the help listing.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Usage syntax (e.g., "/tp &lt;x&gt; &lt;y&gt; &lt;z&gt;").
    /// </summary>
    string Usage { get; }

    /// <summary>
    /// Executes the command with the given arguments.
    /// </summary>
    /// <param name="args">Space-separated arguments (command name not included).</param>
    /// <returns>Result message to display in the console.</returns>
    string Execute(string[] args);
}
