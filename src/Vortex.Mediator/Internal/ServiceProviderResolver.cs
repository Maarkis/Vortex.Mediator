namespace Vortex.Mediator.Internal;

/// <summary>
/// Resolves mediator services from an <see cref="IServiceProvider"/> with cached delegates.
/// </summary>
public static class ServiceProviderResolver
{
    /// <summary>
    /// Resolves a required service from the provider.
    /// </summary>
    /// <typeparam name="T">The service type to resolve.</typeparam>
    /// <param name="provider">The service provider used for resolution.</param>
    /// <returns>The resolved service instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service is not registered.</exception>
    public static T GetRequiredService<T>(IServiceProvider provider) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(provider);
        return RequiredServiceCache<T>.Resolve(provider);
    }

    /// <summary>
    /// Resolves all registered services for a given type.
    /// </summary>
    /// <typeparam name="T">The service type to resolve.</typeparam>
    /// <param name="provider">The service provider used for resolution.</param>
    /// <returns>A read-only list containing the resolved services, or an empty list when none are registered.</returns>
    public static IReadOnlyList<T> GetServices<T>(IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        return ServicesCache<T>.Resolve(provider);
    }

    private static class RequiredServiceCache<T> where T : notnull
    {
        public static readonly Func<IServiceProvider, T> Resolve = CreateResolver();

        private static Func<IServiceProvider, T> CreateResolver()
        {
            return static provider =>
            {
                var service = provider.GetService(typeof(T));
                if (service is T typed)
                {
                    return typed;
                }

                throw new InvalidOperationException($"Service of type '{typeof(T)}' is not registered.");
            };
        }
    }

    private static class ServicesCache<T>
    {
        public static readonly Func<IServiceProvider, IReadOnlyList<T>> Resolve = CreateResolver();

        private static Func<IServiceProvider, IReadOnlyList<T>> CreateResolver()
        {
            return static provider =>
            {
                var services = provider.GetService(typeof(IEnumerable<T>));

                return services switch
                {
                    null => Array.Empty<T>(),
                    T[] array => array,
                    IReadOnlyList<T> readOnlyList => readOnlyList,
                    IEnumerable<T> enumerable => enumerable.ToArray(),
                    _ => Array.Empty<T>()
                };
            };
        }
    }
}