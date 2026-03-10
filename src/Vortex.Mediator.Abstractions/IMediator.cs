namespace Vortex.Mediator.Abstractions;

/// <summary>
/// Defines the entry point for sending requests, publishing notifications, and creating streams.
/// </summary>
public interface IMediator
{
    /// <summary>
    /// Sends a request that produces a response value.
    /// </summary>
    /// <typeparam name="TResponse">The response type produced by the request.</typeparam>
    /// <param name="request">The request instance to dispatch.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that resolves to the response produced by the request handler.</returns>
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a request that does not produce a response value.
    /// </summary>
    /// <param name="request">The request instance to dispatch.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the request handler finishes.</returns>
    Task Send(IRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an asynchronous stream for the specified request.
    /// </summary>
    /// <typeparam name="TResponse">The item type produced by the stream.</typeparam>
    /// <param name="request">The stream request to dispatch.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>An asynchronous sequence produced by the stream handler.</returns>
    IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a notification to all registered handlers.
    /// </summary>
    /// <param name="notification">The notification instance to publish.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when all notification handlers have finished.</returns>
    Task Publish(INotification notification, CancellationToken cancellationToken = default);
}
