using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Vortex.Mediator.Abstractions;

namespace Vortex.Mediator.Tests;

public sealed class CancellationTests
{
    [Test]
    public void SendPropagatesCanceledTokenForResponseRequest()
    {
        var services = CreateServices();
        services.AddScoped<IRequestHandler<CanceledResponseQuery, string>, CanceledResponseQueryHandler>();

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var act = async () => await mediator.Send(new CanceledResponseQuery(), cancellation.Token);

        Assert.That(act, Throws.TypeOf<OperationCanceledException>());
    }

    [Test]
    public void SendPropagatesCanceledTokenForCommand()
    {
        var services = CreateServices();
        services.AddScoped<IRequestHandler<CanceledCommand>, CanceledCommandHandler>();

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var act = async () => await mediator.Send(new CanceledCommand(), cancellation.Token);

        Assert.That(act, Throws.TypeOf<OperationCanceledException>());
    }

    [Test]
    public void CreateStreamPropagatesCanceledToken()
    {
        var services = CreateServices();
        services.AddScoped<IStreamRequestHandler<CanceledStream, int>, CanceledStreamHandler>();

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var act = async () => await DrainAsync(mediator.CreateStream(new CanceledStream(), cancellation.Token));

        Assert.That(act, Throws.TypeOf<OperationCanceledException>());
    }

    [Test]
    public void PublishPropagatesCanceledToken()
    {
        var services = CreateServices();
        services.AddScoped<INotificationHandler<CanceledNotification>, CanceledNotificationHandler>();

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var act = async () => await mediator.Publish(new CanceledNotification(), cancellation.Token);

        Assert.That(act, Throws.TypeOf<OperationCanceledException>());
    }

    private static ServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddVortexMediator();
        return services;
    }

    private static async Task DrainAsync<T>(IAsyncEnumerable<T> source)
    {
        await foreach (var _ in source)
        {
        }
    }

    public sealed record CanceledResponseQuery : IRequest<string>;

    public sealed record CanceledCommand : IRequest;

    public sealed record CanceledStream : IStreamRequest<int>;

    public sealed record CanceledNotification : INotification;

    private sealed class CanceledResponseQueryHandler : IRequestHandler<CanceledResponseQuery, string>
    {
        public Task<string> Handle(CanceledResponseQuery request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(string.Empty);
        }
    }

    private sealed class CanceledCommandHandler : IRequestHandler<CanceledCommand>
    {
        public Task Handle(CanceledCommand request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    private sealed class CanceledStreamHandler : IStreamRequestHandler<CanceledStream, int>
    {
        public async IAsyncEnumerable<int> Handle(CanceledStream request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return 1;
            await Task.Yield();
        }
    }

    private sealed class CanceledNotificationHandler : INotificationHandler<CanceledNotification>
    {
        public Task Handle(CanceledNotification notification, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }
}
