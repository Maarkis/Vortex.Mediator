using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Vortex.Mediator.Abstractions;

namespace Vortex.Mediator.Tests;

public sealed class ExceptionPipelineTests
{
    [Test]
    public void SendPropagatesResponseBehaviorExceptionBeforeNext()
    {
        var services = CreateServices();
        services.AddScoped<IRequestHandler<ResponseRequest, string>, ResponseHandler>();
        services.AddScoped<IPipelineBehavior<ResponseRequest, string>, ThrowingResponseBeforeBehavior>();

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var act = async () => await mediator.Send(new ResponseRequest());

        Assert.That(act, Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void SendPropagatesResponseBehaviorExceptionAfterNext()
    {
        var services = CreateServices();
        services.AddScoped<IRequestHandler<ResponseRequest, string>, ResponseHandler>();
        services.AddScoped<IPipelineBehavior<ResponseRequest, string>, ThrowingResponseAfterBehavior>();

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var act = async () => await mediator.Send(new ResponseRequest());

        Assert.That(act, Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void SendPropagatesCommandBehaviorExceptionBeforeNext()
    {
        var services = CreateServices();
        services.AddScoped<IRequestHandler<CommandRequest>, CommandHandler>();
        services.AddScoped<IPipelineBehavior<CommandRequest>, ThrowingCommandBeforeBehavior>();

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var act = async () => await mediator.Send(new CommandRequest());

        Assert.That(act, Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void SendPropagatesCommandBehaviorExceptionAfterNext()
    {
        var services = CreateServices();
        services.AddScoped<IRequestHandler<CommandRequest>, CommandHandler>();
        services.AddScoped<IPipelineBehavior<CommandRequest>, ThrowingCommandAfterBehavior>();

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var act = async () => await mediator.Send(new CommandRequest());

        Assert.That(act, Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void CreateStreamPropagatesStreamBehaviorException()
    {
        var services = CreateServices();
        services.AddScoped<IStreamRequestHandler<StreamRequest, int>, StreamHandler>();
        services.AddScoped<IStreamPipelineBehavior<StreamRequest, int>, ThrowingStreamBehavior>();

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var act = async () => await DrainAsync(mediator.CreateStream(new StreamRequest()));

        Assert.That(act, Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void SendPropagatesAsyncHandlerException()
    {
        var services = CreateServices();
        services.AddScoped<IRequestHandler<AsyncFailingRequest, string>, AsyncFailingHandler>();

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var act = async () => await mediator.Send(new AsyncFailingRequest());

        Assert.That(act, Throws.TypeOf<ApplicationException>());
    }

    private static ServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddVortexMediator(Enumerable.Empty<System.Reflection.Assembly>());
        return services;
    }

    private static async Task DrainAsync<T>(IAsyncEnumerable<T> source)
    {
        await foreach (var _ in source)
        {
        }
    }

    public sealed record ResponseRequest : IRequest<string>;

    public sealed record CommandRequest : IRequest;

    public sealed record StreamRequest : IStreamRequest<int>;

    public sealed record AsyncFailingRequest : IRequest<string>;

    private sealed class ResponseHandler : IRequestHandler<ResponseRequest, string>
    {
        public Task<string> Handle(ResponseRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult("ok");
        }
    }

    private sealed class CommandHandler : IRequestHandler<CommandRequest>
    {
        public Task Handle(CommandRequest request, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class StreamHandler : IStreamRequestHandler<StreamRequest, int>
    {
        public async IAsyncEnumerable<int> Handle(StreamRequest request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return 1;
            await Task.Yield();
        }
    }

    private sealed class AsyncFailingHandler : IRequestHandler<AsyncFailingRequest, string>
    {
        public async Task<string> Handle(AsyncFailingRequest request, CancellationToken cancellationToken)
        {
            await Task.Yield();
            throw new ApplicationException("async failure");
        }
    }

    private sealed class ThrowingResponseBeforeBehavior : IPipelineBehavior<ResponseRequest, string>
    {
        public Task<string> Handle(ResponseRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("before");
        }
    }

    private sealed class ThrowingResponseAfterBehavior : IPipelineBehavior<ResponseRequest, string>
    {
        public async Task<string> Handle(ResponseRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        {
            _ = await next();
            throw new InvalidOperationException("after");
        }
    }

    private sealed class ThrowingCommandBeforeBehavior : IPipelineBehavior<CommandRequest>
    {
        public Task Handle(CommandRequest request, RequestHandlerDelegate next, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("before");
        }
    }

    private sealed class ThrowingCommandAfterBehavior : IPipelineBehavior<CommandRequest>
    {
        public async Task Handle(CommandRequest request, RequestHandlerDelegate next, CancellationToken cancellationToken)
        {
            await next();
            throw new InvalidOperationException("after");
        }
    }

    private sealed class ThrowingStreamBehavior : IStreamPipelineBehavior<StreamRequest, int>
    {
        public IAsyncEnumerable<int> Handle(StreamRequest request, StreamHandlerDelegate<int> next, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("stream behavior");
        }
    }
}
