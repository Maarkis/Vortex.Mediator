using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Vortex.Mediator.Abstractions;

namespace Vortex.Mediator.Tests;

public sealed class MediatorTests
{
    [Test]
    public void AddVortexMediatorRegistersMediator()
    {
        var services = new ServiceCollection();

        services.AddVortexMediator();
        using var provider = services.BuildServiceProvider();

        Assert.That(provider.GetService<IMediator>(), Is.Not.Null);
    }

    [Test]
    public async Task SendReturnsResponseFromHandler()
    {
        var services = CreateServices();
        services.AddScoped<IRequestHandler<GetNameQuery, string>, GetNameQueryHandler>();

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new GetNameQuery("Ada"));

        Assert.That(response, Is.EqualTo("Hello Ada"));
    }

    [Test]
    public async Task SendExecutesCommandHandler()
    {
        var services = CreateServices();
        services.AddScoped<IRequestHandler<CreateUserCommand>, CreateUserCommandHandler>();

        using var provider = services.BuildServiceProvider();
        var recorder = provider.GetRequiredService<InvocationRecorder>();
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Send(new CreateUserCommand("Ada"));

        Assert.That(recorder.Events, Is.EqualTo(new[] { "command-handler:Ada" }));
    }

    [Test]
    public async Task SendExecutesResponsePipelineInOrder()
    {
        var services = CreateServices();
        services.AddScoped<IRequestHandler<GetNameQuery, string>, GetNameQueryHandler>();
        services.AddScoped<IPipelineBehavior<GetNameQuery, string>, ResponseOuterBehavior>();
        services.AddScoped<IPipelineBehavior<GetNameQuery, string>, ResponseInnerBehavior>();

        using var provider = services.BuildServiceProvider();
        var recorder = provider.GetRequiredService<InvocationRecorder>();
        var mediator = provider.GetRequiredService<IMediator>();

        _ = await mediator.Send(new GetNameQuery("Ada"));

        Assert.That(recorder.Events, Is.EqualTo(new[]
        {
            "response-outer:before",
            "response-inner:before",
            "response-handler:Ada",
            "response-inner:after",
            "response-outer:after"
        }));
    }

    [Test]
    public async Task SendExecutesCommandPipelineInOrder()
    {
        var services = CreateServices();
        services.AddScoped<IRequestHandler<CreateUserCommand>, CreateUserCommandHandler>();
        services.AddScoped<IPipelineBehavior<CreateUserCommand>, CommandOuterBehavior>();
        services.AddScoped<IPipelineBehavior<CreateUserCommand>, CommandInnerBehavior>();

        using var provider = services.BuildServiceProvider();
        var recorder = provider.GetRequiredService<InvocationRecorder>();
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Send(new CreateUserCommand("Ada"));

        Assert.That(recorder.Events, Is.EqualTo(new[]
        {
            "command-outer:before",
            "command-inner:before",
            "command-handler:Ada",
            "command-inner:after",
            "command-outer:after"
        }));
    }

    [Test]
    public async Task CreateStreamReturnsHandlerSequence()
    {
        var services = CreateServices();
        services.AddScoped<IStreamRequestHandler<GetNumbersStream, int>, GetNumbersStreamHandler>();

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var items = await ToListAsync(mediator.CreateStream(new GetNumbersStream(3)));

        Assert.That(items, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public async Task CreateStreamExecutesPipelineInOrder()
    {
        var services = CreateServices();
        services.AddScoped<IStreamRequestHandler<GetNumbersStream, int>, GetNumbersStreamHandler>();
        services.AddScoped<IStreamPipelineBehavior<GetNumbersStream, int>, StreamOuterBehavior>();
        services.AddScoped<IStreamPipelineBehavior<GetNumbersStream, int>, StreamInnerBehavior>();

        using var provider = services.BuildServiceProvider();
        var recorder = provider.GetRequiredService<InvocationRecorder>();
        var mediator = provider.GetRequiredService<IMediator>();

        _ = await ToListAsync(mediator.CreateStream(new GetNumbersStream(2)));

        Assert.That(recorder.Events, Is.EqualTo(new[]
        {
            "stream-outer:before",
            "stream-inner:before",
            "stream-handler:2",
            "stream-inner:after",
            "stream-outer:after"
        }));
    }

    [Test]
    public async Task PublishInvokesAllNotificationHandlers()
    {
        var services = CreateServices();
        services.AddScoped<INotificationHandler<UserCreatedNotification>, AuditNotificationHandler>();
        services.AddScoped<INotificationHandler<UserCreatedNotification>, WelcomeNotificationHandler>();

        using var provider = services.BuildServiceProvider();
        var recorder = provider.GetRequiredService<InvocationRecorder>();
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Publish(new UserCreatedNotification("Ada"));

        Assert.That(recorder.Events, Is.EqualTo(new[]
        {
            "notification-audit:Ada",
            "notification-welcome:Ada"
        }));
    }

    [Test]
    public void SendThrowsWhenHandlerIsMissing()
    {
        var services = CreateServices();

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var act = async () => await mediator.Send(new GetNameQuery("Ada"));

        Assert.That(act, Throws.InvalidOperationException);
    }

    [Test]
    public void SendPropagatesResponseHandlerException()
    {
        var services = CreateServices();
        services.AddScoped<IRequestHandler<FailingResponseQuery, string>, FailingResponseQueryHandler>();

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var act = async () => await mediator.Send(new FailingResponseQuery());

        Assert.That(act, Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void SendPropagatesCommandHandlerException()
    {
        var services = CreateServices();
        services.AddScoped<IRequestHandler<FailingCommand>, FailingCommandHandler>();

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var act = async () => await mediator.Send(new FailingCommand());

        Assert.That(act, Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void CreateStreamPropagatesHandlerException()
    {
        var services = CreateServices();
        services.AddScoped<IStreamRequestHandler<FailingStream, int>, FailingStreamHandler>();

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var act = async () => await ToListAsync(mediator.CreateStream(new FailingStream()));

        Assert.That(act, Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void PublishPropagatesHandlerException()
    {
        var services = CreateServices();
        services.AddScoped<INotificationHandler<FailingNotification>, FailingNotificationHandler>();

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var act = async () => await mediator.Publish(new FailingNotification());

        Assert.That(act, Throws.TypeOf<InvalidOperationException>());
    }

    private static ServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddVortexMediator();
        services.AddSingleton<InvocationRecorder>();
        return services;
    }

    private static async Task<IReadOnlyList<T>> ToListAsync<T>(IAsyncEnumerable<T> source)
    {
        var items = new List<T>();

        await foreach (var item in source)
        {
            items.Add(item);
        }

        return items;
    }

    public sealed record GetNameQuery(string Name) : IRequest<string>;

    public sealed record CreateUserCommand(string Name) : IRequest;

    public sealed record GetNumbersStream(int Count) : IStreamRequest<int>;

    public sealed record UserCreatedNotification(string Name) : INotification;

    public sealed record FailingResponseQuery : IRequest<string>;

    public sealed record FailingCommand : IRequest;

    public sealed record FailingStream : IStreamRequest<int>;

    public sealed record FailingNotification : INotification;

    public sealed class GetNameQueryHandler(InvocationRecorder recorder) : IRequestHandler<GetNameQuery, string>
    {
        public Task<string> Handle(GetNameQuery request, CancellationToken cancellationToken)
        {
            recorder.Events.Add($"response-handler:{request.Name}");
            return Task.FromResult($"Hello {request.Name}");
        }
    }

    public sealed class CreateUserCommandHandler(InvocationRecorder recorder) : IRequestHandler<CreateUserCommand>
    {
        public Task Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            recorder.Events.Add($"command-handler:{request.Name}");
            return Task.CompletedTask;
        }
    }

    public sealed class GetNumbersStreamHandler(InvocationRecorder recorder)
        : IStreamRequestHandler<GetNumbersStream, int>
    {
        public IAsyncEnumerable<int> Handle(GetNumbersStream request, CancellationToken cancellationToken)
        {
            recorder.Events.Add($"stream-handler:{request.Count}");
            return Execute(request.Count, cancellationToken);
        }

        private static async IAsyncEnumerable<int> Execute(int count,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (var index = 1; index <= count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return index;
                await Task.Yield();
            }
        }
    }

    public sealed class AuditNotificationHandler(InvocationRecorder recorder)
        : INotificationHandler<UserCreatedNotification>
    {
        public Task Handle(UserCreatedNotification notification, CancellationToken cancellationToken)
        {
            recorder.Events.Add($"notification-audit:{notification.Name}");
            return Task.CompletedTask;
        }
    }

    public sealed class FailingResponseQueryHandler : IRequestHandler<FailingResponseQuery, string>
    {
        public Task<string> Handle(FailingResponseQuery request, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("response failure");
        }
    }

    public sealed class FailingCommandHandler : IRequestHandler<FailingCommand>
    {
        public Task Handle(FailingCommand request, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("command failure");
        }
    }

    public sealed class FailingStreamHandler : IStreamRequestHandler<FailingStream, int>
    {
        public IAsyncEnumerable<int> Handle(FailingStream request, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("stream failure");
        }
    }

    public sealed class FailingNotificationHandler : INotificationHandler<FailingNotification>
    {
        public Task Handle(FailingNotification notification, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("notification failure");
        }
    }

    public sealed class WelcomeNotificationHandler(InvocationRecorder recorder)
        : INotificationHandler<UserCreatedNotification>
    {
        public Task Handle(UserCreatedNotification notification, CancellationToken cancellationToken)
        {
            recorder.Events.Add($"notification-welcome:{notification.Name}");
            return Task.CompletedTask;
        }
    }

    public sealed class ResponseOuterBehavior(InvocationRecorder recorder) : IPipelineBehavior<GetNameQuery, string>
    {
        public async Task<string> Handle(GetNameQuery request, CancellationToken cancellationToken,
            RequestHandlerDelegate<string> next)
        {
            recorder.Events.Add("response-outer:before");
            var response = await next();
            recorder.Events.Add("response-outer:after");
            return response;
        }
    }

    public sealed class ResponseInnerBehavior(InvocationRecorder recorder) : IPipelineBehavior<GetNameQuery, string>
    {
        public async Task<string> Handle(GetNameQuery request, CancellationToken cancellationToken,
            RequestHandlerDelegate<string> next)
        {
            recorder.Events.Add("response-inner:before");
            var response = await next();
            recorder.Events.Add("response-inner:after");
            return response;
        }
    }

    public sealed class CommandOuterBehavior(InvocationRecorder recorder) : IPipelineBehavior<CreateUserCommand>
    {
        public async Task Handle(CreateUserCommand request, CancellationToken cancellationToken,
            RequestHandlerDelegate next)
        {
            recorder.Events.Add("command-outer:before");
            await next();
            recorder.Events.Add("command-outer:after");
        }
    }

    public sealed class CommandInnerBehavior(InvocationRecorder recorder) : IPipelineBehavior<CreateUserCommand>
    {
        public async Task Handle(CreateUserCommand request, CancellationToken cancellationToken,
            RequestHandlerDelegate next)
        {
            recorder.Events.Add("command-inner:before");
            await next();
            recorder.Events.Add("command-inner:after");
        }
    }

    public sealed class StreamOuterBehavior(InvocationRecorder recorder)
        : IStreamPipelineBehavior<GetNumbersStream, int>
    {
        public IAsyncEnumerable<int> Handle(GetNumbersStream request, CancellationToken cancellationToken,
            StreamHandlerDelegate<int> next)
        {
            recorder.Events.Add("stream-outer:before");
            var stream = next();
            recorder.Events.Add("stream-outer:after");
            return stream;
        }
    }

    public sealed class StreamInnerBehavior(InvocationRecorder recorder)
        : IStreamPipelineBehavior<GetNumbersStream, int>
    {
        public IAsyncEnumerable<int> Handle(GetNumbersStream request, CancellationToken cancellationToken,
            StreamHandlerDelegate<int> next)
        {
            recorder.Events.Add("stream-inner:before");
            var stream = next();
            recorder.Events.Add("stream-inner:after");
            return stream;
        }
    }

    public sealed class InvocationRecorder
    {
        public List<string> Events { get; } = [];
    }
}
