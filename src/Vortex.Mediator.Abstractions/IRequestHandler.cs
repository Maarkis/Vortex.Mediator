namespace Vortex.Mediator.Abstractions;

/// <summary>
/// Handles a request that does not produce a response value.
/// </summary>
/// <typeparam name="TRequest">The request type handled by this implementation.</typeparam>
public interface IRequestHandler<in TRequest> where TRequest : IRequest
{
    /// <summary>
    /// Handles the specified request.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the request has been handled.</returns>
    Task Handle(TRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Handles a request that produces a response value.
/// </summary>
/// <typeparam name="TRequest">The request type handled by this implementation.</typeparam>
/// <typeparam name="TResponse">The response type produced by the request.</typeparam>
public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Handles the specified request.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that resolves to the response produced by the handler.</returns>
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
