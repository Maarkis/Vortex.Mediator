using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Vortex.Mediator.Abstractions;

namespace Vortex.Mediator.Tests;

public sealed class NullHandlingTests
{
    [Test]
    public void SendThrowsWhenGenericRequestIsNull()
    {
        using var provider = CreateServices().BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        IRequest<string>? request = null;
        var act = async () => await mediator.Send(request!);

        Assert.That(act, Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void SendThrowsWhenCommandIsNull()
    {
        using var provider = CreateServices().BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        IRequest? request = null;
        var act = async () => await mediator.Send(request!);

        Assert.That(act, Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void CreateStreamThrowsWhenRequestIsNull()
    {
        using var provider = CreateServices().BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        IStreamRequest<string?>? request = null;
        var act = () => mediator.CreateStream(request!);

        Assert.That(act, Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void PublishThrowsWhenNotificationIsNull()
    {
        using var provider = CreateServices().BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        INotification? notification = null;
        var act = async () => await mediator.Publish(notification!);

        Assert.That(act, Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public async Task SendSupportsNullClassResponse()
    {
        var services = CreateServices();
        services.AddScoped<IRequestHandler<NullClassQuery, NullDto?>, NullClassQueryHandler>();

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new NullClassQuery());

        Assert.That(response, Is.Null);
    }

    [Test]
    public async Task CreateStreamSupportsNullItems()
    {
        var services = CreateServices();
        services.AddScoped<IStreamRequestHandler<NullItemStreamQuery, string?>, NullItemStreamQueryHandler>();

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var items = await ToListAsync(mediator.CreateStream(new NullItemStreamQuery()));

        Assert.That(items[0], Is.Null);
    }

    private static ServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddVortexMediator();
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

    public sealed record NullClassQuery : IRequest<NullDto?>;

    public sealed record NullItemStreamQuery : IStreamRequest<string?>;

    public sealed class NullDto;

    private sealed class NullClassQueryHandler : IRequestHandler<NullClassQuery, NullDto?>
    {
        public Task<NullDto?> Handle(NullClassQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult<NullDto?>(null);
        }
    }

    private sealed class NullItemStreamQueryHandler : IStreamRequestHandler<NullItemStreamQuery, string?>
    {
        public async IAsyncEnumerable<string?> Handle(NullItemStreamQuery request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return null;
            await Task.Yield();
        }
    }
}
