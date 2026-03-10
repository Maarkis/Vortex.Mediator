namespace Vortex.Mediator.Abstractions;

/// <summary>
/// Handles a stream request.
/// </summary>
/// <typeparam name="TRequest">The stream request type handled by this implementation.</typeparam>
/// <typeparam name="TResponse">The item type produced by the stream.</typeparam>
public interface IStreamRequestHandler<in TRequest, out TResponse> where TRequest : IStreamRequest<TResponse>
{
    /// <summary>
    /// Handles the specified stream request.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>An asynchronous sequence produced by the handler.</returns>
    IAsyncEnumerable<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
