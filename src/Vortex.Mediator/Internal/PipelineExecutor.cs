using Vortex.Mediator.Abstractions;

namespace Vortex.Mediator.Internal;

public static class PipelineExecutor
{
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
            return behavior.Handle(request, cancellationToken, next);
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
            return behavior.Handle(request, cancellationToken, next);
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
            return behavior.Handle(request, cancellationToken, next);
        }
    }
}