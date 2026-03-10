namespace Vortex.Mediator.Abstractions;

public delegate IAsyncEnumerable<TResponse> StreamHandlerDelegate<out TResponse>();