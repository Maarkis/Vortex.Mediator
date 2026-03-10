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

        return Invoke(behaviors.Count - 1);

        Task<TResponse> Invoke(int index)
        {
            if (index < 0)
            {
                return handler.Handle(request, cancellationToken);
            }

            return behaviors[index].Handle(request, cancellationToken, () => Invoke(index - 1));
        }
    }
}