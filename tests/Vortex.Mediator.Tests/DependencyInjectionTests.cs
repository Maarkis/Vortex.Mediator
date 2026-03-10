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
    public async Task AddVortexMediatorExecutesStaticRequestHandlerAutomatically()
    {
        var services = new ServiceCollection();
        services.AddSingleton<AutoRecorder>();
        services.AddVortexMediator(typeof(DependencyInjectionTests).Assembly);

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new StaticQuery("Ada"));

        Assert.That(response, Is.EqualTo("static:Ada"));
    }

    [Test]
    public async Task AddVortexMediatorExecutesStaticCommandHandlerAutomatically()
    {
        var services = new ServiceCollection();
        services.AddSingleton<AutoRecorder>();
        services.AddVortexMediator(typeof(DependencyInjectionTests).Assembly);

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var recorder = provider.GetRequiredService<AutoRecorder>();
        await mediator.Send(new StaticCommand("Ada"));

        Assert.That(recorder.Events, Is.EqualTo(new[] { "static-command:Ada" }));
    }

    [Test]
    public async Task AddVortexMediatorExecutesStaticNotificationHandlerAutomatically()
    {
        var services = new ServiceCollection();
        services.AddSingleton<AutoRecorder>();
        services.AddVortexMediator(typeof(DependencyInjectionTests).Assembly);

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var recorder = provider.GetRequiredService<AutoRecorder>();
        await mediator.Publish(new StaticNotification("Ada"));

        Assert.That(recorder.Events, Is.EqualTo(new[] { "static-notification:Ada" }));
    }

    [Test]
    public async Task AddVortexMediatorExecutesStaticStreamHandlerAutomatically()
    {
        var services = new ServiceCollection();
        services.AddSingleton<AutoRecorder>();
        services.AddVortexMediator(typeof(DependencyInjectionTests).Assembly);

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var items = await ToListAsync(mediator.CreateStream(new StaticStream(2)));

        Assert.That(items, Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public async Task AddVortexMediatorExecutesInstanceRequestHandlerAutomatically()
    {
        var services = new ServiceCollection();
        services.AddSingleton<AutoRecorder>();
        services.AddVortexMediator(typeof(DependencyInjectionTests).Assembly);

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new InstanceQuery("Ada"));

        Assert.That(response, Is.EqualTo("instance:Ada"));
    }

    [Test]
    public async Task AddVortexMediatorUsesRegisteredInstanceHandlerWhenAvailable()
    {
        var services = new ServiceCollection();
        services.AddSingleton<AutoRecorder>();
        services.AddSingleton<RegisteredInstanceQueryEndpoint>();
        services.AddVortexMediator(typeof(DependencyInjectionTests).Assembly);

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new RegisteredInstanceQuery("Ada"));

        Assert.That(response, Is.EqualTo("registered-instance:Ada"));
    }

    [Test]
    public async Task AddVortexMediatorExecutesInstanceCommandHandlerAutomatically()
    {
        var services = new ServiceCollection();
        services.AddSingleton<AutoRecorder>();
        services.AddVortexMediator(typeof(DependencyInjectionTests).Assembly);

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var recorder = provider.GetRequiredService<AutoRecorder>();
        await mediator.Send(new InstanceCommand("Ada"));

        Assert.That(recorder.Events, Is.EqualTo(new[] { "instance-command:Ada" }));
    }

    [Test]
    public async Task AddVortexMediatorExecutesInstanceNotificationHandlerAutomatically()
    {
        var services = new ServiceCollection();
        services.AddSingleton<AutoRecorder>();
        services.AddVortexMediator(typeof(DependencyInjectionTests).Assembly);

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var recorder = provider.GetRequiredService<AutoRecorder>();
        await mediator.Publish(new InstanceNotification("Ada"));

        Assert.That(recorder.Events, Is.EqualTo(new[] { "instance-notification:Ada" }));
    }

    [Test]
    public async Task AddVortexMediatorExecutesInstanceStreamHandlerAutomatically()
    {
        var services = new ServiceCollection();
        services.AddSingleton<AutoRecorder>();
        services.AddVortexMediator(typeof(DependencyInjectionTests).Assembly);

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var items = await ToListAsync(mediator.CreateStream(new InstanceStream(2)));

        Assert.That(items, Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public async Task AddVortexMediatorStaticStreamHandlerCanUseInjectedService()
    {
        var services = new ServiceCollection();
        services.AddSingleton<AutoRecorder>();
        services.AddVortexMediator(typeof(DependencyInjectionTests).Assembly);

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var recorder = provider.GetRequiredService<AutoRecorder>();
        _ = await ToListAsync(mediator.CreateStream(new StaticStream(2)));

        Assert.That(recorder.Events, Is.EqualTo(new[] { "static-stream:1", "static-stream:2" }));
    }

    [Test]
    public async Task AddVortexMediatorPrefersStaticRequestHandlerOverInterfaceHandler()
    {
        var services = new ServiceCollection();
        services.AddSingleton<AutoRecorder>();
        services.AddVortexMediator(typeof(DependencyInjectionTests).Assembly);

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new DualQuery("Ada"));

        Assert.That(response, Is.EqualTo("static-dual:Ada"));
    }

    [Test]
    public async Task AddVortexMediatorCombinesStaticAndInterfaceNotificationHandlers()
    {
        var services = new ServiceCollection();
        services.AddSingleton<AutoRecorder>();
        services.AddVortexMediator(typeof(DependencyInjectionTests).Assembly);

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var recorder = provider.GetRequiredService<AutoRecorder>();
        await mediator.Publish(new MixedNotification("Ada"));

        Assert.That(recorder.Events, Is.EqualTo(new[] { "mixed-interface:Ada", "mixed-static:Ada" }));
    }

    [Test]
    public void AddVortexMediatorThrowsWhenStaticHandlerDependencyIsMissing()
    {
        var services = new ServiceCollection();
        services.AddVortexMediator(typeof(DependencyInjectionTests).Assembly);

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var act = async () => await mediator.Send(new MissingDependencyQuery("Ada"));

        Assert.That(act, Throws.InvalidOperationException);
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
    public sealed record StaticQuery(string Name) : IRequest<string>;
    public sealed record StaticCommand(string Name) : IRequest;
    public sealed record StaticNotification(string Name) : INotification;
    public sealed record StaticStream(int Count) : IStreamRequest<int>;
    public sealed record InstanceQuery(string Name) : IRequest<string>;
    public sealed record RegisteredInstanceQuery(string Name) : IRequest<string>;
    public sealed record InstanceCommand(string Name) : IRequest;
    public sealed record InstanceNotification(string Name) : INotification;
    public sealed record InstanceStream(int Count) : IStreamRequest<int>;
    public sealed record DualQuery(string Name) : IRequest<string>;
    public sealed record MixedNotification(string Name) : INotification;
    public sealed record MissingDependencyQuery(string Name) : IRequest<string>;

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

    public sealed class DualQueryHandler : IRequestHandler<DualQuery, string>
    {
        public Task<string> Handle(DualQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult($"interface-dual:{request.Name}");
        }
    }

    public sealed class MixedNotificationHandler(AutoRecorder recorder) : INotificationHandler<MixedNotification>
    {
        public Task Handle(MixedNotification notification, CancellationToken cancellationToken)
        {
            recorder.Events.Add($"mixed-interface:{notification.Name}");
            return Task.CompletedTask;
        }
    }

    public sealed class MissingDependency;

    public static class StaticQueryEndpoint
    {
        public static Task<string> Handle(StaticQuery request, AutoRecorder recorder, CancellationToken cancellationToken)
        {
            recorder.Events.Add($"static-query:{request.Name}");
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult($"static:{request.Name}");
        }
    }

    public static class StaticCommandEndpoint
    {
        public static Task Handle(StaticCommand request, AutoRecorder recorder, CancellationToken cancellationToken)
        {
            recorder.Events.Add($"static-command:{request.Name}");
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    public static class StaticNotificationEndpoint
    {
        public static Task Handle(StaticNotification notification, AutoRecorder recorder, CancellationToken cancellationToken)
        {
            recorder.Events.Add($"static-notification:{notification.Name}");
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    public static class StaticStreamEndpoint
    {
        public static async IAsyncEnumerable<int> Handle(
            StaticStream request,
            AutoRecorder recorder,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (var index = 1; index <= request.Count; index++)
            {
                recorder.Events.Add($"static-stream:{index}");
                cancellationToken.ThrowIfCancellationRequested();
                yield return index;
                await Task.Yield();
            }
        }
    }

    public sealed class InstanceQueryEndpoint(AutoRecorder recorder)
    {
        public Task<string> Handle(InstanceQuery request, CancellationToken cancellationToken)
        {
            recorder.Events.Add($"instance-query:{request.Name}");
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult($"instance:{request.Name}");
        }
    }

    public sealed class RegisteredInstanceQueryEndpoint(AutoRecorder recorder)
    {
        public Task<string> Handle(RegisteredInstanceQuery request, CancellationToken cancellationToken)
        {
            recorder.Events.Add($"registered-instance-query:{request.Name}");
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult($"registered-instance:{request.Name}");
        }
    }

    public sealed class InstanceCommandEndpoint(AutoRecorder recorder)
    {
        public Task Handle(InstanceCommand request, CancellationToken cancellationToken)
        {
            recorder.Events.Add($"instance-command:{request.Name}");
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    public sealed class InstanceNotificationEndpoint(AutoRecorder recorder)
    {
        public Task Handle(InstanceNotification notification, CancellationToken cancellationToken)
        {
            recorder.Events.Add($"instance-notification:{notification.Name}");
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    public sealed class InstanceStreamEndpoint(AutoRecorder recorder)
    {
        public async IAsyncEnumerable<int> Handle(
            InstanceStream request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (var index = 1; index <= request.Count; index++)
            {
                recorder.Events.Add($"instance-stream:{index}");
                cancellationToken.ThrowIfCancellationRequested();
                yield return index;
                await Task.Yield();
            }
        }
    }

    public static class DualQueryEndpoint
    {
        public static Task<string> Handle(DualQuery request, AutoRecorder recorder, CancellationToken cancellationToken)
        {
            recorder.Events.Add($"dual-static:{request.Name}");
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult($"static-dual:{request.Name}");
        }
    }

    public static class MixedNotificationEndpoint
    {
        public static Task Handle(MixedNotification notification, AutoRecorder recorder, CancellationToken cancellationToken)
        {
            recorder.Events.Add($"mixed-static:{notification.Name}");
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    public static class MissingDependencyEndpoint
    {
        public static Task<string> Handle(
            MissingDependencyQuery request,
            MissingDependency dependency,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(request.Name);
        }
    }
}
