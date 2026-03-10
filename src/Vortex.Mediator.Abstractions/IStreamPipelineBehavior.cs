namespace Vortex.Mediator.Abstractions;

public interface IStreamPipelineBehavior<TRequest, TResponse>
{
    IAsyncEnumerable<TResponse> Handle(TRequest request, CancellationToken cancellationToken,
        StreamHandlerDelegate<TResponse> next);
}