namespace Vortex.Mediator.Abstractions;

/// <summary>
/// Defines a pipeline behavior for stream requests.
/// </summary>
/// <typeparam name="TRequest">The stream request type processed by the behavior.</typeparam>
/// <typeparam name="TResponse">The item type produced by the stream.</typeparam>
public interface IStreamPipelineBehavior<TRequest, TResponse>
{
    /// <summary>
    /// Handles the specified stream request and invokes the next component in the stream pipeline.
    /// </summary>
    /// <param name="request">The request being processed.</param>
    /// <param name="next">The delegate that invokes the next behavior or final stream handler.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>An asynchronous sequence produced by the pipeline.</returns>
    IAsyncEnumerable<TResponse> Handle(TRequest request, StreamHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken);
}
