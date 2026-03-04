using System.Diagnostics.CodeAnalysis;

namespace MineRPG.Core.DI;

/// <summary>
/// Service locator interface. ONLY use in Godot bridge projects where constructor
/// injection is impossible. Pure projects must use constructor injection exclusively.
/// </summary>
public interface IServiceLocator
{
    /// <summary>
    /// Register a service. Throws <see cref="InvalidOperationException"/> if already registered.
    /// Use <see cref="Replace{T}"/> for intentional overwrites.
    /// </summary>
    void Register<T>(T instance) where T : class;

    /// <summary>
    /// Replace an existing registration, or register if none exists.
    /// Intended for test setup and hot-reload scenarios.
    /// </summary>
    void Replace<T>(T instance) where T : class;

    /// <summary>
    /// Throws <see cref="InvalidOperationException"/> if nothing is registered.
    /// </summary>
    T Get<T>() where T : class;

    bool TryGet<T>([NotNullWhen(true)] out T? service) where T : class;
}
