using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace MineRPG.Core.DI;

/// <summary>
/// Global service locator. Wired in GameBootstrapper at startup.
/// ONLY for Godot bridge _Ready() methods. Pure projects use constructor injection.
/// </summary>
public sealed class ServiceLocator : IServiceLocator
{
    private static IServiceLocator? _instance;

    public static IServiceLocator Instance
        => _instance ?? throw new InvalidOperationException(
            "ServiceLocator has not been initialized. " +
            "Call ServiceLocator.SetInstance() from GameBootstrapper before use.");

    /// <summary>
    /// Set the global instance. Called once from GameBootstrapper.
    /// Subsequent calls replace the instance (useful for test setup).
    /// </summary>
    public static void SetInstance(IServiceLocator locator) => _instance = locator ?? throw new ArgumentNullException(nameof(locator));

    /// <summary>
    /// Reset the global instance to null. For test teardown only.
    /// </summary>
    public static void ResetInstance() => _instance = null;

    private readonly ConcurrentDictionary<Type, object> _services = new();

    public void Register<T>(T instance) where T : class
    {
        if (instance is null)
            throw new ArgumentNullException(nameof(instance));

        if (!_services.TryAdd(typeof(T), instance))
            throw new InvalidOperationException(
                $"Service '{typeof(T).FullName}' is already registered. " +
                "Use Replace<T>() for intentional overwrites.");
    }

    public void Replace<T>(T instance) where T : class
        => _services[typeof(T)] = instance ?? throw new ArgumentNullException(nameof(instance));

    public T Get<T>() where T : class
    {
        if (_services.TryGetValue(typeof(T), out var obj))
            return (T)obj;

        throw new InvalidOperationException(
            $"Service '{typeof(T).FullName}' has not been registered in the ServiceLocator.");
    }

    public bool TryGet<T>([NotNullWhen(true)] out T? service) where T : class
    {
        if (_services.TryGetValue(typeof(T), out var obj))
        {
            service = (T)obj;
            return true;
        }

        service = null;
        return false;
    }
}
