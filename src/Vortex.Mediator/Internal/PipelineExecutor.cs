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
}
