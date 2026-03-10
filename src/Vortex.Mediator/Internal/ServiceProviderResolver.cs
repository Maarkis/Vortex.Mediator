namespace Vortex.Mediator.Internal;

public static class ServiceProviderResolver
{
    public static T GetRequiredService<T>(IServiceProvider provider) where T : notnull
    {
        var service = provider.GetService(typeof(T));
        if (service is T typed)
        {
            return typed;
        }

        throw new InvalidOperationException($"Service of type '{typeof(T)}' is not registered.");
    }

    public static IReadOnlyList<T> GetServices<T>(IServiceProvider provider)
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
    }
}