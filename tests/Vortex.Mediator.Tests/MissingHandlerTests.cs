using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Vortex.Mediator.Abstractions;

namespace Vortex.Mediator.Tests;

public sealed class MissingHandlerTests
{
    [Test]
    public void SendThrowsWhenCommandHandlerIsMissing()
    {
        using var provider = CreateServices().BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var act = async () => await mediator.Send(new MissingCommand());

        Assert.That(act, Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void CreateStreamThrowsWhenHandlerIsMissing()
    {
        using var provider = CreateServices().BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var act = async () => await DrainAsync(mediator.CreateStream(new MissingStream()));

        Assert.That(act, Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public async Task PublishReturnsCompletedTaskWhenHandlersAreMissing()
    {
        using var provider = CreateServices().BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var task = mediator.Publish(new MissingNotification());

        await task;

        Assert.That(task.IsCompletedSuccessfully, Is.True);
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

    public sealed record MissingCommand : IRequest;
    public sealed record MissingStream : IStreamRequest<int>;
    public sealed record MissingNotification : INotification;
}
