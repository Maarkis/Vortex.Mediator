using Vortex.Mediator.Abstractions;

namespace Vortex.Mediator;

public interface IMediatorBinding
{
    bool TryDispatch<TResponse>(
        IRequest<TResponse> request,
        IServiceProvider provider,
        CancellationToken cancellationToken,
        out Task<TResponse>? task);

    bool TryPublish(
        INotification notification,
        IServiceProvider provider,
        CancellationToken cancellationToken,
        out Task? task);
}
