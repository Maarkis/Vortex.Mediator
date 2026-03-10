using Vortex.Mediator.Abstractions;

namespace Vortex.Mediator;

public interface IMediatorBinding
{
    bool TryDispatch<TResponse>(
        IRequest<TResponse> request,
        IServiceProvider provider,
        CancellationToken cancellationToken,
        out Task<TResponse>? task);

    bool TryDispatch(
        IRequest request,
        IServiceProvider provider,
        CancellationToken cancellationToken,
        out Task? task);

    bool TryCreateStream<TResponse>(
        IStreamRequest<TResponse> request,
        IServiceProvider provider,
        CancellationToken cancellationToken,
        out IAsyncEnumerable<TResponse>? stream);

    bool TryPublish(
        INotification notification,
        IServiceProvider provider,
        CancellationToken cancellationToken,
        out Task? task);
}
