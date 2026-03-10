namespace Vortex.Mediator.Abstractions;

/// <summary>
/// Represents a request that does not produce a response value.
/// </summary>
public interface IRequest
{
}

/// <summary>
/// Represents a request that produces a response value.
/// </summary>
/// <typeparam name="TResponse">The response type produced by the request.</typeparam>
public interface IRequest<TResponse>
{
}
