# Vortex.Mediator

`Vortex.Mediator` is a source-generated mediator library for .NET with support for:

- request/response with `Task<T>`
- commands with `Task`
- notifications with multiple handlers
- async streams with `IAsyncEnumerable<T>`
- pipeline behaviors
- convention-based handlers with compile-time dispatch

The runtime package is [`Vortex.Mediator`](./src/Vortex.Mediator). It already brings [`Vortex.Mediator.Abstractions`](./src/Vortex.Mediator.Abstractions) as a transitive dependency.

## Installation

For normal application usage, install only `Vortex.Mediator`.

```bash
dotnet add package Vortex.Mediator
```

If you need only the public contracts in a shared contracts project, install `Vortex.Mediator.Abstractions`.

```bash
dotnet add package Vortex.Mediator.Abstractions
```

## Registering In DI

```csharp
using Vortex.Mediator.DependencyInjection;

builder.Services.AddVortexMediator();
```

You can also scan explicit assemblies:

```csharp
builder.Services.AddVortexMediator(typeof(Program).Assembly);
```

## Usage Modes

### 1. Interface-Based Handler

```csharp
using Vortex.Mediator.Abstractions;

public sealed record CreateUserCommand(string Name) : IRequest<Guid>;

public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Guid>
{
    public Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Guid.NewGuid());
    }
}
```

### 2. Convention-Based Static Handler

```csharp
using Vortex.Mediator.Abstractions;

public sealed record CreateUserCommand(string Name) : IRequest<Guid>;

public static class CreateUserEndpoint
{
    public static Task<Guid> Handle(
        CreateUserCommand command,
        IUserRepository repository,
        CancellationToken cancellationToken)
    {
        return repository.CreateAsync(command.Name, cancellationToken);
    }
}
```

### 3. Convention-Based Instance Handler

```csharp
using Vortex.Mediator.Abstractions;

public sealed record CreateUserCommand(string Name) : IRequest<Guid>;

public sealed class CreateUserEndpoint(IUserRepository repository)
{
    public Task<Guid> Handle(
        CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        return repository.CreateAsync(command.Name, cancellationToken);
    }
}
```

For instance handlers, the generated code:

- first tries to resolve the handler from DI
- if it is not registered, falls back to `ActivatorUtilities.CreateInstance`

## Sending Requests

```csharp
var userId = await mediator.Send(new CreateUserCommand("Ada"), cancellationToken);
```

Command without response:

```csharp
public sealed record RebuildCacheCommand() : IRequest;

await mediator.Send(new RebuildCacheCommand(), cancellationToken);
```

## Publishing Notifications

```csharp
public sealed record UserCreated(Guid UserId) : INotification;

await mediator.Publish(new UserCreated(userId), cancellationToken);
```

Interface-based notification handler:

```csharp
public sealed class SendWelcomeEmailHandler : INotificationHandler<UserCreated>
{
    public Task Handle(UserCreated notification, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
```

Convention-based notification handler:

```csharp
public static class UserCreatedEndpoint
{
    public static Task Handle(UserCreated notification, IEmailSender emailSender, CancellationToken cancellationToken)
    {
        return emailSender.SendWelcomeAsync(notification.UserId, cancellationToken);
    }
}
```

## Streams

```csharp
public sealed record GetNumbers(int Count) : IStreamRequest<int>;

public static class NumberStreamEndpoint
{
    public static async IAsyncEnumerable<int> Handle(
        GetNumbers request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (var index = 1; index <= request.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return index;
            await Task.Yield();
        }
    }
}
```

Consumption:

```csharp
await foreach (var item in mediator.CreateStream(new GetNumbers(3), cancellationToken))
{
    Console.WriteLine(item);
}
```

## Pipeline Behaviors

Response pipeline:

```csharp
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        Console.WriteLine(typeof(TRequest).Name);
        return await next();
    }
}
```

Registration:

```csharp
builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
```

The same pattern exists for:

- `IPipelineBehavior<TRequest>` for commands
- `IStreamPipelineBehavior<TRequest, TResponse>` for streams

## Notes

- `Vortex.Mediator` uses source generation for dispatch.
- Requests, notifications and streams are mapped at compile time.
- Reflection is not used in the runtime dispatch path.
- Convention-based handlers and interface-based handlers can coexist.

## Development

- Main runtime package: [`src/Vortex.Mediator`](./src/Vortex.Mediator)
- Public contracts: [`src/Vortex.Mediator.Abstractions`](./src/Vortex.Mediator.Abstractions)
- Source generator: [`src/Vortex.Mediator.SourceGenerator`](./src/Vortex.Mediator.SourceGenerator)
- Tests: [`tests/Vortex.Mediator.Tests`](./tests/Vortex.Mediator.Tests)

See [CONTRIBUTING.md](./CONTRIBUTING.md) for contribution guidelines.
