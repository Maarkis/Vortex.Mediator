namespace Vortex.Mediator.Abstractions;

/// <summary>
/// Defines a pipeline behavior for requests that do not produce a response value.
/// </summary>
/// <typeparam name="TRequest">The request type processed by the behavior.</typeparam>
public interface IPipelineBehavior<TRequest>
{
    /// <summary>
    /// Handles the specified request and invokes the next component in the pipeline.
    /// </summary>
    /// <param name="request">The request being processed.</param>
    /// <param name="next">The delegate that invokes the next behavior or final handler.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the pipeline finishes.</returns>
    Task Handle(TRequest request, RequestHandlerDelegate next,
        CancellationToken cancellationToken);
}

/// <summary>
/// Defines a pipeline behavior for requests that produce a response value.
/// </summary>
/// <typeparam name="TRequest">The request type processed by the behavior.</typeparam>
/// <typeparam name="TResponse">The response type produced by the request.</typeparam>
public interface IPipelineBehavior<TRequest, TResponse>
{
    /// <summary>
    /// Handles the specified request and invokes the next component in the pipeline.
    /// </summary>
    /// <param name="request">The request being processed.</param>
    /// <param name="next">The delegate that invokes the next behavior or final handler.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that resolves to the response produced by the pipeline.</returns>
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken);
}
