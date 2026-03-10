using Vortex.Mediator.Abstractions;

namespace Vortex.Mediator;

/// <summary>
/// Default mediator implementation backed by generated dispatch bindings.
/// </summary>
public sealed class Mediator : IMediator
{
    private readonly IServiceProvider _provider;
    private static readonly Lazy<IReadOnlyList<IMediatorBinding>> Bindings = new(LoadBindings);

    /// <summary>
    /// Initializes a new instance of the <see cref="Mediator"/> class.
    /// </summary>
    /// <param name="provider">The service provider used to resolve handlers and behaviors.</param>
    public Mediator(IServiceProvider provider)
    {
        _provider = provider;
    }

    /// <inheritdoc />
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return DispatchResponse(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task Send(IRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Dispatch(request, cancellationToken);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return DispatchStream(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task Publish(INotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        return DispatchNotification(notification, cancellationToken);
    }

    private Task<TResponse> DispatchResponse<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken)
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

    private Task Dispatch(IRequest request, CancellationToken cancellationToken)
    {
        var bindings = Bindings.Value;

        for (var index = 0; index < bindings.Count; index++)
        {
            if (bindings[index].TryDispatch(request, _provider, cancellationToken, out Task? task))
            {
                return task!;
            }
        }

        throw new InvalidOperationException($"No handler mapping was generated for '{request.GetType()}'.");
    }

    private IAsyncEnumerable<TResponse> DispatchStream<TResponse>(
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken)
    {
        var bindings = Bindings.Value;

        for (var index = 0; index < bindings.Count; index++)
        {
            if (bindings[index].TryCreateStream(request, _provider, cancellationToken, out IAsyncEnumerable<TResponse>? stream))
            {
                return stream!;
            }
        }

        throw new InvalidOperationException($"No stream handler mapping was generated for '{request.GetType()}'.");
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
