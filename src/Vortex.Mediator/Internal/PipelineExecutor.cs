using Vortex.Mediator.Abstractions;

namespace Vortex.Mediator.Internal;

/// <summary>
/// Executes mediator handler pipelines for requests, commands, and streams.
/// </summary>
public static class PipelineExecutor
{
    /// <summary>
    /// Executes a request/response handler with its registered pipeline behaviors.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="request">The request instance to execute.</param>
    /// <param name="cancellationToken">The cancellation token for the current operation.</param>
    /// <param name="handler">The request handler.</param>
    /// <param name="provider">The service provider used to resolve behaviors.</param>
    /// <returns>A task that completes with the handler response.</returns>
    public static Task<TResponse> Execute<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken,
        IRequestHandler<TRequest, TResponse> handler, IServiceProvider provider) where TRequest : IRequest<TResponse>
    {
        var behaviors = ServiceProviderResolver.GetServices<IPipelineBehavior<TRequest, TResponse>>(provider);

        if (behaviors.Count == 0)
        {
            return handler.Handle(request, cancellationToken);
        }

        RequestHandlerDelegate<TResponse> next =
            new HandlerInvocation<TRequest, TResponse>(request, cancellationToken, handler).Invoke;

        for (var index = behaviors.Count - 1; index >= 0; index--)
        {
            next = new BehaviorInvocation<TRequest, TResponse>(
                request,
                cancellationToken,
                behaviors[index],
                next).Invoke;
        }

        return next();
    }

    /// <summary>
    /// Executes a command handler with its registered pipeline behaviors.
    /// </summary>
    /// <typeparam name="TRequest">The command type.</typeparam>
    /// <param name="request">The command instance to execute.</param>
    /// <param name="cancellationToken">The cancellation token for the current operation.</param>
    /// <param name="handler">The command handler.</param>
    /// <param name="provider">The service provider used to resolve behaviors.</param>
    /// <returns>A task that completes when the command has been handled.</returns>
    public static Task Execute<TRequest>(TRequest request, CancellationToken cancellationToken,
        IRequestHandler<TRequest> handler, IServiceProvider provider) where TRequest : IRequest
    {
        var behaviors = ServiceProviderResolver.GetServices<IPipelineBehavior<TRequest>>(provider);

        if (behaviors.Count == 0)
        {
            return handler.Handle(request, cancellationToken);
        }

        RequestHandlerDelegate next =
            new CommandHandlerInvocation<TRequest>(request, cancellationToken, handler).Invoke;

        for (var index = behaviors.Count - 1; index >= 0; index--)
        {
            next = new CommandBehaviorInvocation<TRequest>(
                request,
                cancellationToken,
                behaviors[index],
                next).Invoke;
        }

        return next();
    }

    /// <summary>
    /// Executes a stream handler with its registered stream pipeline behaviors.
    /// </summary>
    /// <typeparam name="TRequest">The stream request type.</typeparam>
    /// <typeparam name="TResponse">The stream element type.</typeparam>
    /// <param name="request">The stream request instance to execute.</param>
    /// <param name="cancellationToken">The cancellation token for the current operation.</param>
    /// <param name="handler">The stream handler.</param>
    /// <param name="provider">The service provider used to resolve behaviors.</param>
    /// <returns>The resulting asynchronous stream.</returns>
    public static IAsyncEnumerable<TResponse> ExecuteStream<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken,
        IStreamRequestHandler<TRequest, TResponse> handler,
        IServiceProvider provider)
        where TRequest : IStreamRequest<TResponse>
    {
        var behaviors = ServiceProviderResolver.GetServices<IStreamPipelineBehavior<TRequest, TResponse>>(provider);

        if (behaviors.Count == 0)
        {
            return handler.Handle(request, cancellationToken);
        }

        StreamHandlerDelegate<TResponse> next =
            new StreamHandlerInvocation<TRequest, TResponse>(request, cancellationToken, handler).Invoke;

        for (var index = behaviors.Count - 1; index >= 0; index--)
        {
            next = new StreamBehaviorInvocation<TRequest, TResponse>(
                request,
                cancellationToken,
                behaviors[index],
                next).Invoke;
        }

        return next();
    }

    private sealed class HandlerInvocation<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken,
        IRequestHandler<TRequest, TResponse> handler)
        where TRequest : IRequest<TResponse>
    {
        public Task<TResponse> Invoke()
        {
            return handler.Handle(request, cancellationToken);
        }
    }

    private sealed class BehaviorInvocation<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken,
        IPipelineBehavior<TRequest, TResponse> behavior,
        RequestHandlerDelegate<TResponse> next)
    {
        public Task<TResponse> Invoke()
        {
            return behavior.Handle(request, next, cancellationToken);
        }
    }

    private sealed class CommandHandlerInvocation<TRequest>(
        TRequest request,
        CancellationToken cancellationToken,
        IRequestHandler<TRequest> handler)
        where TRequest : IRequest
    {
        public Task Invoke()
        {
            return handler.Handle(request, cancellationToken);
        }
    }

    private sealed class CommandBehaviorInvocation<TRequest>(
        TRequest request,
        CancellationToken cancellationToken,
        IPipelineBehavior<TRequest> behavior,
        RequestHandlerDelegate next)
        where TRequest : IRequest
    {
        public Task Invoke()
        {
            return behavior.Handle(request, next, cancellationToken);
        }
    }

    private sealed class StreamHandlerInvocation<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken,
        IStreamRequestHandler<TRequest, TResponse> handler)
        where TRequest : IStreamRequest<TResponse>
    {
        public IAsyncEnumerable<TResponse> Invoke()
        {
            return handler.Handle(request, cancellationToken);
        }
    }

    private sealed class StreamBehaviorInvocation<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken,
        IStreamPipelineBehavior<TRequest, TResponse> behavior,
        StreamHandlerDelegate<TResponse> next)
        where TRequest : IStreamRequest<TResponse>
    {
        public IAsyncEnumerable<TResponse> Invoke()
        {
            return behavior.Handle(request, next, cancellationToken);
        }
    }
}
