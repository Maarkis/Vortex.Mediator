using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Vortex.Mediator.Abstractions;

namespace Vortex.Mediator.Tests;

public sealed class GenericEdgeTypeTests
{
    [Test]
    public async Task SendSupportsNullableStringResponse()
    {
        var services = CreateServices();
        services.AddScoped<IRequestHandler<NullableStringQuery, string?>, NullableStringQueryHandler>();

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new NullableStringQuery());

        Assert.That(response, Is.Null);
    }

    [Test]
    public async Task SendSupportsNullableIntResponse()
    {
        var services = CreateServices();
        services.AddScoped<IRequestHandler<NullableIntQuery, int?>, NullableIntQueryHandler>();

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new NullableIntQuery());

        Assert.That(response, Is.EqualTo(7));
    }

    [Test]
    public async Task SendSupportsEnumResponse()
    {
        var services = CreateServices();
        services.AddScoped<IRequestHandler<EnumQuery, SampleState>, EnumQueryHandler>();

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new EnumQuery());

        Assert.That(response, Is.EqualTo(SampleState.Active));
    }

    [Test]
    public async Task SendSupportsStructResponse()
    {
        var services = CreateServices();
        services.AddScoped<IRequestHandler<StructQuery, SampleStruct>, StructQueryHandler>();

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new StructQuery());

        Assert.That(response.Value, Is.EqualTo(11));
    }

    [Test]
    public async Task SendSupportsListResponse()
    {
        var services = CreateServices();
        services.AddScoped<IRequestHandler<ListQuery, List<string>>, ListQueryHandler>();

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new ListQuery());

        Assert.That(response, Is.EqualTo(new[] { "a", "b" }));
    }

    [Test]
    public async Task SendSupportsDictionaryResponse()
    {
        var services = CreateServices();
        services.AddScoped<IRequestHandler<DictionaryQuery, Dictionary<string, int>>, DictionaryQueryHandler>();

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new DictionaryQuery());

        Assert.That(response["a"], Is.EqualTo(1));
    }

    [Test]
    public async Task CreateStreamSupportsEmptySequence()
    {
        var services = CreateServices();
        services.AddScoped<IStreamRequestHandler<EmptyStreamQuery, int>, EmptyStreamQueryHandler>();

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var items = await ToListAsync(mediator.CreateStream(new EmptyStreamQuery()));

        Assert.That(items, Is.Empty);
    }

    [Test]
    public async Task SendSupportsNestedRecordResponse()
    {
        var services = CreateServices();
        services.AddScoped<IRequestHandler<Container.NestedRecordQuery, Container.NestedRecordResponse>, NestedRecordHandler>();

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new Container.NestedRecordQuery("Ada"));

        Assert.That(response, Is.EqualTo(new Container.NestedRecordResponse("Ada")));
    }

    [Test]
    public async Task SendSupportsNestedClassResponse()
    {
        var services = CreateServices();
        services.AddScoped<IRequestHandler<Container.NestedClassQuery, Container.NestedClassResponse>, NestedClassHandler>();

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new Container.NestedClassQuery("Ada"));

        Assert.That(response.Name, Is.EqualTo("Ada"));
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

    public sealed record NullableStringQuery : IRequest<string?>;
    public sealed record NullableIntQuery : IRequest<int?>;
    public sealed record EnumQuery : IRequest<SampleState>;
    public sealed record StructQuery : IRequest<SampleStruct>;
    public sealed record ListQuery : IRequest<List<string>>;
    public sealed record DictionaryQuery : IRequest<Dictionary<string, int>>;
    public sealed record EmptyStreamQuery : IStreamRequest<int>;

    public enum SampleState
    {
        Unknown,
        Active
    }

    public readonly record struct SampleStruct(int Value);

    public static class Container
    {
        public sealed record NestedRecordQuery(string Name) : IRequest<NestedRecordResponse>;
        public sealed record NestedRecordResponse(string Name);
        public sealed record NestedClassQuery(string Name) : IRequest<NestedClassResponse>;

        public sealed class NestedClassResponse
        {
            public required string Name { get; init; }
        }
    }

    private sealed class NullableStringQueryHandler : IRequestHandler<NullableStringQuery, string?>
    {
        public Task<string?> Handle(NullableStringQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult<string?>(null);
        }
    }

    private sealed class NullableIntQueryHandler : IRequestHandler<NullableIntQuery, int?>
    {
        public Task<int?> Handle(NullableIntQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult<int?>(7);
        }
    }

    private sealed class EnumQueryHandler : IRequestHandler<EnumQuery, SampleState>
    {
        public Task<SampleState> Handle(EnumQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult(SampleState.Active);
        }
    }

    private sealed class StructQueryHandler : IRequestHandler<StructQuery, SampleStruct>
    {
        public Task<SampleStruct> Handle(StructQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new SampleStruct(11));
        }
    }

    private sealed class ListQueryHandler : IRequestHandler<ListQuery, List<string>>
    {
        public Task<List<string>> Handle(ListQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new List<string> { "a", "b" });
        }
    }

    private sealed class DictionaryQueryHandler : IRequestHandler<DictionaryQuery, Dictionary<string, int>>
    {
        public Task<Dictionary<string, int>> Handle(DictionaryQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new Dictionary<string, int> { ["a"] = 1 });
        }
    }

    private sealed class EmptyStreamQueryHandler : IStreamRequestHandler<EmptyStreamQuery, int>
    {
        public async IAsyncEnumerable<int> Handle(EmptyStreamQuery request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    private sealed class NestedRecordHandler : IRequestHandler<Container.NestedRecordQuery, Container.NestedRecordResponse>
    {
        public Task<Container.NestedRecordResponse> Handle(Container.NestedRecordQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new Container.NestedRecordResponse(request.Name));
        }
    }

    private sealed class NestedClassHandler : IRequestHandler<Container.NestedClassQuery, Container.NestedClassResponse>
    {
        public Task<Container.NestedClassResponse> Handle(Container.NestedClassQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new Container.NestedClassResponse { Name = request.Name });
        }
    }
}
