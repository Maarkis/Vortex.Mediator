using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Vortex.Mediator.Abstractions;

namespace Vortex.Mediator.Tests;

public sealed class GenericResponseTypeTests
{
    [Test]
    public async Task SendSupportsStringResponse()
    {
        var services = CreateServices();
        services.AddScoped<IRequestHandler<StringQuery, string>, StringQueryHandler>();

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new StringQuery("Ada"));

        Assert.That(response, Is.EqualTo("Ada"));
    }

    [Test]
    public async Task SendSupportsIntResponse()
    {
        var services = CreateServices();
        services.AddScoped<IRequestHandler<IntQuery, int>, IntQueryHandler>();

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new IntQuery(21));

        Assert.That(response, Is.EqualTo(42));
    }

    [Test]
    public async Task SendSupportsDoubleResponse()
    {
        var services = CreateServices();
        services.AddScoped<IRequestHandler<DoubleQuery, double>, DoubleQueryHandler>();

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new DoubleQuery(10.5d));

        Assert.That(response, Is.EqualTo(21d));
    }

    [Test]
    public async Task SendSupportsClassResponse()
    {
        var services = CreateServices();
        services.AddScoped<IRequestHandler<ClassQuery, PersonDto>, ClassQueryHandler>();

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new ClassQuery("Ada"));

        Assert.That(response.Name, Is.EqualTo("Ada"));
    }

    [Test]
    public async Task SendSupportsRecordResponse()
    {
        var services = CreateServices();
        services.AddScoped<IRequestHandler<RecordQuery, PersonRecord>, RecordQueryHandler>();

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new RecordQuery("Ada"));

        Assert.That(response, Is.EqualTo(new PersonRecord("Ada")));
    }

    [Test]
    public async Task CreateStreamSupportsDoubleResponse()
    {
        var services = CreateServices();
        services.AddScoped<IStreamRequestHandler<DoubleStreamQuery, double>, DoubleStreamQueryHandler>();

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var items = await ToListAsync(mediator.CreateStream(new DoubleStreamQuery(2.5d)));

        Assert.That(items, Is.EqualTo(new[] { 2.5d, 5d }));
    }

    [Test]
    public async Task CreateStreamSupportsRecordResponse()
    {
        var services = CreateServices();
        services.AddScoped<IStreamRequestHandler<RecordStreamQuery, PersonRecord>, RecordStreamQueryHandler>();

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var items = await ToListAsync(mediator.CreateStream(new RecordStreamQuery("Ada")));

        Assert.That(items, Is.EqualTo(new[] { new PersonRecord("Ada"), new PersonRecord("Ada-2") }));
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

    public sealed record StringQuery(string Value) : IRequest<string>;

    public sealed record IntQuery(int Value) : IRequest<int>;

    public sealed record DoubleQuery(double Value) : IRequest<double>;

    public sealed record ClassQuery(string Name) : IRequest<PersonDto>;

    public sealed record RecordQuery(string Name) : IRequest<PersonRecord>;

    public sealed record DoubleStreamQuery(double Value) : IStreamRequest<double>;

    public sealed record RecordStreamQuery(string Name) : IStreamRequest<PersonRecord>;

    public sealed class PersonDto
    {
        public required string Name { get; init; }
    }

    public sealed record PersonRecord(string Name);

    private sealed class StringQueryHandler : IRequestHandler<StringQuery, string>
    {
        public Task<string> Handle(StringQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult(request.Value);
        }
    }

    private sealed class IntQueryHandler : IRequestHandler<IntQuery, int>
    {
        public Task<int> Handle(IntQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult(request.Value * 2);
        }
    }

    private sealed class DoubleQueryHandler : IRequestHandler<DoubleQuery, double>
    {
        public Task<double> Handle(DoubleQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult(request.Value * 2);
        }
    }

    private sealed class ClassQueryHandler : IRequestHandler<ClassQuery, PersonDto>
    {
        public Task<PersonDto> Handle(ClassQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new PersonDto { Name = request.Name });
        }
    }

    private sealed class RecordQueryHandler : IRequestHandler<RecordQuery, PersonRecord>
    {
        public Task<PersonRecord> Handle(RecordQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new PersonRecord(request.Name));
        }
    }

    private sealed class DoubleStreamQueryHandler : IStreamRequestHandler<DoubleStreamQuery, double>
    {
        public IAsyncEnumerable<double> Handle(DoubleStreamQuery request, CancellationToken cancellationToken)
        {
            return Execute(request.Value, cancellationToken);
        }

        private static async IAsyncEnumerable<double> Execute(
            double value,
            [System.Runtime.CompilerServices.EnumeratorCancellation]
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return value;
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return value * 2;
        }
    }

    private sealed class RecordStreamQueryHandler : IStreamRequestHandler<RecordStreamQuery, PersonRecord>
    {
        public IAsyncEnumerable<PersonRecord> Handle(RecordStreamQuery request, CancellationToken cancellationToken)
        {
            return Execute(request.Name, cancellationToken);
        }

        private static async IAsyncEnumerable<PersonRecord> Execute(
            string name,
            [System.Runtime.CompilerServices.EnumeratorCancellation]
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new PersonRecord(name);
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return new PersonRecord($"{name}-2");
        }
    }
}