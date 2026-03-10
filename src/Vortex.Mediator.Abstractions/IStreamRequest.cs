namespace Vortex.Mediator.Abstractions;

/// <summary>
/// Represents a request that produces an asynchronous stream of items.
/// </summary>
/// <typeparam name="TResponse">The item type produced by the stream.</typeparam>
public interface IStreamRequest<TResponse>;
