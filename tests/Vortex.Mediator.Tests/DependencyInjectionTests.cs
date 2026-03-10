using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Vortex.Mediator.Abstractions;

namespace Vortex.Mediator.Tests;

public sealed class DependencyInjectionTests
{
    [Test]
    public async Task AddVortexMediatorRegistersRequestHandlersAutomatically()
    {
        var services = new ServiceCollection();
        services.AddVortexMediator(typeof(DependencyInjectionTests).Assembly);

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new AutoQuery("Ada"));

        Assert.That(response, Is.EqualTo("auto:Ada"));
    }

    [Test]
    public async Task AddVortexMediatorRegistersCommandHandlersAutomatically()
    {
        var services = new ServiceCollection();
        services.AddSingleton<AutoRecorder>();
        services.AddVortexMediator(typeof(DependencyInjectionTests).Assembly);

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var recorder = provider.GetRequiredService<AutoRecorder>();
        await mediator.Send(new AutoCommand("Ada"));

        Assert.That(recorder.Events, Is.EqualTo(new[] { "command:Ada" }));
    }

    [Test]
    public async Task AddVortexMediatorRegistersNotificationHandlersAutomatically()
    {
        var services = new ServiceCollection();
        services.AddSingleton<AutoRecorder>();
        services.AddVortexMediator(typeof(DependencyInjectionTests).Assembly);

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var recorder = provider.GetRequiredService<AutoRecorder>();
        await mediator.Publish(new AutoNotification("Ada"));

        Assert.That(recorder.Events, Is.EqualTo(new[] { "notification:Ada" }));
    }

    [Test]
    public async Task AddVortexMediatorRegistersStreamHandlersAutomatically()
    {
        var services = new ServiceCollection();
        services.AddVortexMediator(typeof(DependencyInjectionTests).Assembly);

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var items = await ToListAsync(mediator.CreateStream(new AutoStream(2)));

        Assert.That(items, Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public async Task AddVortexMediatorRegistersBehaviorsAutomatically()
    {
        var services = new ServiceCollection();
        services.AddSingleton<AutoRecorder>();
        services.AddVortexMediator(typeof(DependencyInjectionTests).Assembly);

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var recorder = provider.GetRequiredService<AutoRecorder>();
        _ = await mediator.Send(new AutoBehaviorQuery("Ada"));

        Assert.That(recorder.Events, Is.EqualTo(new[]
        {
            "before",
            "handler:Ada",
            "after"
        }));
    }

    [Test]
    public async Task AddVortexMediatorDoesNotDuplicateRegistrationsForRepeatedAssemblies()
    {
        var services = new ServiceCollection();
        services.AddVortexMediator(typeof(DependencyInjectionTests).Assembly, typeof(DependencyInjectionTests).Assembly);

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new AutoQuery("Ada"));

        Assert.That(response, Is.EqualTo("auto:Ada"));
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

    public sealed record AutoQuery(string Name) : IRequest<string>;
    public sealed record AutoCommand(string Name) : IRequest;
    public sealed record AutoNotification(string Name) : INotification;
    public sealed record AutoStream(int Count) : IStreamRequest<int>;
    public sealed record AutoBehaviorQuery(string Name) : IRequest<string>;

    public sealed class AutoQueryHandler : IRequestHandler<AutoQuery, string>
    {
        public Task<string> Handle(AutoQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult($"auto:{request.Name}");
        }
    }

    public sealed class AutoCommandHandler(AutoRecorder recorder) : IRequestHandler<AutoCommand>
    {
        public Task Handle(AutoCommand request, CancellationToken cancellationToken)
        {
            recorder.Events.Add($"command:{request.Name}");
            return Task.CompletedTask;
        }
    }

    public sealed class AutoNotificationHandler(AutoRecorder recorder) : INotificationHandler<AutoNotification>
    {
        public Task Handle(AutoNotification notification, CancellationToken cancellationToken)
        {
            recorder.Events.Add($"notification:{notification.Name}");
            return Task.CompletedTask;
        }
    }

    public sealed class AutoStreamHandler : IStreamRequestHandler<AutoStream, int>
    {
        public async IAsyncEnumerable<int> Handle(
            AutoStream request,
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

    public sealed class AutoBehaviorQueryHandler(AutoRecorder recorder) : IRequestHandler<AutoBehaviorQuery, string>
    {
        public Task<string> Handle(AutoBehaviorQuery request, CancellationToken cancellationToken)
        {
            recorder.Events.Add($"handler:{request.Name}");
            return Task.FromResult(request.Name);
        }
    }

    public sealed class AutoBehavior(AutoRecorder recorder) : IPipelineBehavior<AutoBehaviorQuery, string>
    {
        public async Task<string> Handle(AutoBehaviorQuery request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        {
            recorder.Events.Add("before");
            var response = await next();
            recorder.Events.Add("after");
            return response;
        }
    }

    public sealed class AutoRecorder
    {
        public List<string> Events { get; } = [];
    }
}
