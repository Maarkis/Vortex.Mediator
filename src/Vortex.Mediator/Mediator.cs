using Vortex.Mediator.Abstractions;

namespace Vortex.Mediator;

public sealed class Mediator : IMediator
{
    private readonly IServiceProvider _provider;
    private static readonly Lazy<IReadOnlyList<IMediatorBinding>> Bindings = new(LoadBindings);

    public Mediator(IServiceProvider provider)
    {
        _provider = provider;
    }

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Dispatch(request, cancellationToken);
    }

    public Task Publish(INotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        return DispatchNotification(notification, cancellationToken);
    }

    private Task<TResponse> Dispatch<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken)
    {
        var bindings = Bindings.Value;

        for (var index = 0; index < bindings.Count; index++)
        {
            if (bindings[index].TryDispatch(request, _provider, cancellationToken, out Task<TResponse>? task))
            {
                return task!;
            }
        }

        throw new InvalidOperationException($"No handler mapping was generated for '{request.GetType()}'.");
    }

    private Task DispatchNotification(INotification notification, CancellationToken cancellationToken)
    {
        var bindings = Bindings.Value;

        for (var index = 0; index < bindings.Count; index++)
        {
            if (bindings[index].TryPublish(notification, _provider, cancellationToken, out Task? task))
            {
                return task!;
            }
        }

        return Task.CompletedTask;
    }

    private static IReadOnlyList<IMediatorBinding> LoadBindings()
    {
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
        var bindings = new List<IMediatorBinding>();
        var seenTypes = new HashSet<Type>();

        for (var assemblyIndex = 0; assemblyIndex < loadedAssemblies.Length; assemblyIndex++)
        {
            var attributes = loadedAssemblies[assemblyIndex]
                .GetCustomAttributes(typeof(MediatorBindingAttribute), false);

            for (var attributeIndex = 0; attributeIndex < attributes.Length; attributeIndex++)
            {
                if (attributes[attributeIndex] is not MediatorBindingAttribute attribute ||
                    !seenTypes.Add(attribute.BindingType))
                {
                    continue;
                }

                if (Activator.CreateInstance(attribute.BindingType) is IMediatorBinding binding)
                {
                    bindings.Add(binding);
                }
            }
        }

        return bindings;
    }
}
