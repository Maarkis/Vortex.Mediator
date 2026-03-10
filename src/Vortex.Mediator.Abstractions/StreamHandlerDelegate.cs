namespace Vortex.Mediator.Abstractions;

/// <summary>
/// Represents the next delegate in a stream pipeline.
/// </summary>
/// <typeparam name="TResponse">The item type produced by the stream.</typeparam>
/// <returns>An asynchronous sequence produced by the next pipeline component.</returns>
public delegate IAsyncEnumerable<TResponse> StreamHandlerDelegate<out TResponse>();
