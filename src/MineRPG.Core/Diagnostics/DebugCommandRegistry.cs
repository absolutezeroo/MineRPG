using System;
using System.Collections.Generic;

namespace MineRPG.Core.Diagnostics;

/// <summary>
/// Registry for debug commands. Supports registration, lookup, autocomplete,
/// and execution. Data-driven — adding a command requires no switch statements.
/// </summary>
public sealed class DebugCommandRegistry
{
    private readonly Dictionary<string, IDebugCommand> _commands = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _sortedNames = new();

    /// <summary>
    /// Registers a debug command. Throws if a command with the same name already exists.
    /// </summary>
    /// <param name="command">The command to register.</param>
    public void Register(IDebugCommand command)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        if (_commands.ContainsKey(command.Name))
        {
            throw new InvalidOperationException($"Debug command '{command.Name}' is already registered.");
        }

        _commands[command.Name] = command;
        _sortedNames.Add(command.Name);
        _sortedNames.Sort(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Executes a raw command string (e.g., "/tp 100 64 200").
    /// </summary>
    /// <param name="input">The full command string, optionally starting with '/'.</param>
    /// <returns>The result message from the command, or an error message.</returns>
    public string Execute(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "Empty command.";
        }

        string trimmed = input.TrimStart('/').Trim();
        string[] parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
        {
            return "Empty command.";
        }

        string commandName = parts[0];

        if (!_commands.TryGetValue(commandName, out IDebugCommand? command) || command is null)
        {
            return $"Unknown command: '{commandName}'. Type /help for a list.";
        }

        string[] args = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

        try
        {
            return command.Execute(args);
        }
        catch (Exception ex)
        {
            return $"Error executing '{commandName}': {ex.Message}";
        }
    }

    /// <summary>
    /// Returns autocomplete suggestions for the given partial input.
    /// </summary>
    /// <param name="partial">The partial command name typed so far.</param>
    /// <returns>List of matching command names.</returns>
    public IReadOnlyList<string> GetAutocompleteSuggestions(string partial)
    {
        List<string> suggestions = new();
        string trimmed = partial.TrimStart('/').Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            suggestions.AddRange(_sortedNames);
            return suggestions;
        }

        for (int i = 0; i < _sortedNames.Count; i++)
        {
            if (_sortedNames[i].StartsWith(trimmed, StringComparison.OrdinalIgnoreCase))
            {
                suggestions.Add(_sortedNames[i]);
            }
        }

        return suggestions;
    }

    /// <summary>
    /// Gets all registered commands for help display.
    /// </summary>
    /// <returns>Read-only collection of all commands.</returns>
    public IReadOnlyList<IDebugCommand> GetAll()
    {
        List<IDebugCommand> all = new(_commands.Count);

        for (int i = 0; i < _sortedNames.Count; i++)
        {
            all.Add(_commands[_sortedNames[i]]);
        }

        return all;
    }

    /// <summary>
    /// Gets the number of registered commands.
    /// </summary>
    public int Count => _commands.Count;
}
