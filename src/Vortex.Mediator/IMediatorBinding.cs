using Vortex.Mediator.Abstractions;

namespace Vortex.Mediator;

/// <summary>
/// Defines the generated binding contract used by <see cref="Mediator"/> to dispatch supported messages.
/// </summary>
public interface IMediatorBinding
{
    /// <summary>
    /// Attempts to dispatch a request/response message to a generated handler pipeline.
    /// </summary>
    /// <typeparam name="TResponse">The response type produced by the request.</typeparam>
    /// <param name="request">The request instance to dispatch.</param>
    /// <param name="provider">The service provider used to resolve handlers and behaviors.</param>
    /// <param name="cancellationToken">The cancellation token for the current operation.</param>
    /// <param name="task">When this method returns <see langword="true"/>, contains the dispatch task.</param>
    /// <returns><see langword="true"/> when the binding can handle the request; otherwise, <see langword="false"/>.</returns>
    bool TryDispatch<TResponse>(
        IRequest<TResponse> request,
        IServiceProvider provider,
        CancellationToken cancellationToken,
        out Task<TResponse>? task);

    /// <summary>
    /// Attempts to dispatch a command message to a generated handler pipeline.
    /// </summary>
    /// <param name="request">The command instance to dispatch.</param>
    /// <param name="provider">The service provider used to resolve handlers and behaviors.</param>
    /// <param name="cancellationToken">The cancellation token for the current operation.</param>
    /// <param name="task">When this method returns <see langword="true"/>, contains the dispatch task.</param>
    /// <returns><see langword="true"/> when the binding can handle the request; otherwise, <see langword="false"/>.</returns>
    bool TryDispatch(
        IRequest request,
        IServiceProvider provider,
        CancellationToken cancellationToken,
        out Task? task);

    /// <summary>
    /// Attempts to dispatch a streaming request to a generated handler pipeline.
    /// </summary>
    /// <typeparam name="TResponse">The element type yielded by the stream.</typeparam>
    /// <param name="request">The stream request instance to dispatch.</param>
    /// <param name="provider">The service provider used to resolve handlers and behaviors.</param>
    /// <param name="cancellationToken">The cancellation token for the current operation.</param>
    /// <param name="stream">When this method returns <see langword="true"/>, contains the generated stream.</param>
    /// <returns><see langword="true"/> when the binding can handle the request; otherwise, <see langword="false"/>.</returns>
    bool TryCreateStream<TResponse>(
        IStreamRequest<TResponse> request,
        IServiceProvider provider,
        CancellationToken cancellationToken,
        out IAsyncEnumerable<TResponse>? stream);

    /// <summary>
    /// Attempts to publish a notification to generated notification handlers.
    /// </summary>
    /// <param name="notification">The notification instance to publish.</param>
    /// <param name="provider">The service provider used to resolve handlers.</param>
    /// <param name="cancellationToken">The cancellation token for the current operation.</param>
    /// <param name="task">When this method returns <see langword="true"/>, contains the publication task.</param>
    /// <returns><see langword="true"/> when the binding can handle the notification; otherwise, <see langword="false"/>.</returns>
    bool TryPublish(
        INotification notification,
        IServiceProvider provider,
        CancellationToken cancellationToken,
        out Task? task);
}
