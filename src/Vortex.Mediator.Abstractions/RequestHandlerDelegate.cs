namespace Vortex.Mediator.Abstractions;

/// <summary>
/// Represents the next delegate in a request pipeline that does not produce a response value.
/// </summary>
/// <returns>A task that completes when the next pipeline component finishes.</returns>
public delegate Task RequestHandlerDelegate();

/// <summary>
/// Represents the next delegate in a request pipeline that produces a response value.
/// </summary>
/// <typeparam name="TResponse">The response type produced by the next pipeline component.</typeparam>
/// <returns>A task that resolves to the response produced by the next pipeline component.</returns>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();
