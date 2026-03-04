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
    /// <typeparam name="T">The service type to register.</typeparam>
    /// <param name="instance">The service instance to register.</param>
    void Register<T>(T instance) where T : class;

    /// <summary>
    /// Replace an existing registration, or register if none exists.
    /// Intended for test setup and hot-reload scenarios.
    /// </summary>
    /// <typeparam name="T">The service type to replace.</typeparam>
    /// <param name="instance">The new service instance.</param>
    void Replace<T>(T instance) where T : class;

    /// <summary>
    /// Retrieve a registered service.
    /// Throws <see cref="InvalidOperationException"/> if nothing is registered.
    /// </summary>
    /// <typeparam name="T">The service type to retrieve.</typeparam>
    /// <returns>The registered service instance.</returns>
    T Get<T>() where T : class;

    /// <summary>
    /// Try to retrieve a registered service without throwing.
    /// </summary>
    /// <typeparam name="T">The service type to retrieve.</typeparam>
    /// <param name="service">The service instance if found; null otherwise.</param>
    /// <returns>True if the service was found.</returns>
    bool TryGet<T>([NotNullWhen(true)] out T? service) where T : class;
}
