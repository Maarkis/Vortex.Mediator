namespace Vortex.Mediator.Abstractions;

public interface IPipelineBehavior<TRequest>
{
    Task Handle(TRequest request, CancellationToken cancellationToken,
        RequestHandlerDelegate next);
}

public interface IPipelineBehavior<TRequest, TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken,
        RequestHandlerDelegate<TResponse> next);
}
