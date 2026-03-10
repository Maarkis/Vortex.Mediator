namespace Vortex.Mediator.Internal;

public static class ServiceProviderResolver
{
    public static T GetRequiredService<T>(IServiceProvider provider) where T : notnull
    {
        return RequiredServiceCache<T>.Resolve(provider);
    }

    public static IReadOnlyList<T> GetServices<T>(IServiceProvider provider)
    {
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
