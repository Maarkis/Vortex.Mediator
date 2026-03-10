using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using Vortex.Mediator;
using Vortex.Mediator.SourceGenerator;

namespace Vortex.Mediator.Tests;

public sealed class SourceGeneratorTests
{
    [Test]
    public void GeneratorPreservesNullableReferenceTypes()
    {
        var source = """
using Vortex.Mediator.Abstractions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Demo;

public sealed record Query() : IRequest<string?>;

public sealed class QueryHandler : IRequestHandler<Query, string?>
{
    public Task<string?> Handle(Query request, CancellationToken cancellationToken) => Task.FromResult<string?>(null);
}
""";

        var generated = GenerateSource(source);

        Assert.That(generated, Does.Contain("string?"));
    }

    [Test]
    public void GeneratorSupportsNestedAndExternalTypes()
    {
        var source = """
using Vortex.Mediator.Abstractions;

namespace Demo.External;

public static class Container
{
    public sealed record NestedQuery(string Name) : IRequest<NestedResponse>;
    public sealed record NestedResponse(string Name);
}

namespace Demo.Handlers;

public sealed class NestedHandler : IRequestHandler<Demo.External.Container.NestedQuery, Demo.External.Container.NestedResponse>
{
    public Task<Demo.External.Container.NestedResponse> Handle(Demo.External.Container.NestedQuery request, CancellationToken cancellationToken)
        => Task.FromResult(new Demo.External.Container.NestedResponse(request.Name));
}
""";

        var generated = GenerateSource(source);

        Assert.That(generated, Does.Contain("global::Demo.External.Container.NestedQuery"));
    }

    [Test]
    public void GeneratorSupportsGenericRecordResponses()
    {
        var source = """
using Vortex.Mediator.Abstractions;

namespace Demo;

public sealed record Envelope<T>(T Value);

public sealed record EnvelopeQuery(int Value) : IRequest<Envelope<int>>;

public sealed class EnvelopeHandler : IRequestHandler<EnvelopeQuery, Envelope<int>>
{
    public Task<Envelope<int>> Handle(EnvelopeQuery request, CancellationToken cancellationToken)
        => Task.FromResult(new Envelope<int>(request.Value));
}
""";

        var generated = GenerateSource(source);

        Assert.That(generated, Does.Contain("global::Demo.Envelope<int>"));
    }

    [Test]
    public void GeneratorProducesCompilableOutput()
    {
        var source = """
using Vortex.Mediator.Abstractions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Demo;

public sealed record PingQuery(string Value) : IRequest<string>;
public sealed record PingCommand(string Value) : IRequest;
public sealed record PingStream(int Count) : IStreamRequest<int>;
public sealed record PingNotification(string Value) : INotification;

public sealed class PingQueryHandler : IRequestHandler<PingQuery, string>
{
    public Task<string> Handle(PingQuery request, CancellationToken cancellationToken) => Task.FromResult(request.Value);
}

public sealed class PingCommandHandler : IRequestHandler<PingCommand>
{
    public Task Handle(PingCommand request, CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class PingStreamHandler : IStreamRequestHandler<PingStream, int>
{
    public async IAsyncEnumerable<int> Handle(PingStream request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return request.Count;
        await Task.Yield();
    }
}

public sealed class PingNotificationHandler : INotificationHandler<PingNotification>
{
    public Task Handle(PingNotification notification, CancellationToken cancellationToken) => Task.CompletedTask;
}
""";

        var outputCompilation = RunGenerator(source, out _);
        var diagnostics = outputCompilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => diagnostic.ToString())
            .ToArray();

        Assert.That(diagnostics, Is.Empty, string.Join(Environment.NewLine, diagnostics));
    }

    [Test]
    public void GeneratorSupportsStaticRequestHandlersWithInjectedServices()
    {
        var source = """
using Vortex.Mediator.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace Demo;

public sealed record PingQuery(string Value) : IRequest<string>;

public sealed class QueryDependency;

public static class PingEndpoint
{
    public static Task<string> Handle(PingQuery request, QueryDependency dependency, CancellationToken cancellationToken)
        => Task.FromResult(request.Value);
}
""";

        var generated = GenerateSource(source);

        Assert.That(generated, Does.Contain("GetRequiredService<global::Demo.QueryDependency>(provider)"));
    }

    [Test]
    public void GeneratorSupportsStaticNotificationHandlers()
    {
        var source = """
using Vortex.Mediator.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace Demo;

public sealed record PingNotification(string Value) : INotification;

public sealed class NotificationDependency;

public static class PingEndpoint
{
    public static Task Handle(PingNotification notification, NotificationDependency dependency, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
""";

        var generated = GenerateSource(source);

        Assert.That(generated, Does.Contain("MethodNotificationHandler"));
    }

    [Test]
    public void GeneratorSupportsStaticCommandHandlersWithInjectedServices()
    {
        var source = """
using Vortex.Mediator.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace Demo;

public sealed record PingCommand(string Value) : IRequest;

public sealed class CommandDependency;

public static class PingEndpoint
{
    public static Task Handle(PingCommand command, CommandDependency dependency, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
""";

        var generated = GenerateSource(source);

        Assert.That(generated, Does.Contain("MethodCommandHandler"));
    }

    [Test]
    public void GeneratorSupportsStaticStreamHandlersWithInjectedServices()
    {
        var source = """
using Vortex.Mediator.Abstractions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Demo;

public sealed record PingStream(int Count) : IStreamRequest<int>;

public sealed class StreamDependency;

public static class PingEndpoint
{
    public static async IAsyncEnumerable<int> Handle(PingStream request, StreamDependency dependency, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return request.Count;
        await Task.Yield();
    }
}
""";

        var generated = GenerateSource(source);

        Assert.That(generated, Does.Contain("MethodStreamHandler"));
    }

    [Test]
    public void GeneratorSupportsInstanceRequestHandlers()
    {
        var source = """
using Vortex.Mediator.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace Demo;

public sealed record PingQuery(string Value) : IRequest<string>;

public sealed class QueryEndpoint
{
    public Task<string> Handle(PingQuery request, CancellationToken cancellationToken)
        => Task.FromResult(request.Value);
}
""";

        var generated = GenerateSource(source);

        Assert.That(generated, Does.Contain("GetOrCreateInstance<global::Demo.QueryEndpoint>(provider)"));
    }

    [Test]
    public void GeneratorSupportsInstanceNotificationHandlers()
    {
        var source = """
using Vortex.Mediator.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace Demo;

public sealed record PingNotification(string Value) : INotification;

public sealed class NotificationEndpoint
{
    public Task Handle(PingNotification notification, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
""";

        var generated = GenerateSource(source);

        Assert.That(generated, Does.Contain("GetOrCreateInstance<global::Demo.NotificationEndpoint>(provider)"));
    }

    private static string GenerateSource(string source)
    {
        _ = RunGenerator(source, out var generated);
        return generated;
    }

    private static CSharpCompilation RunGenerator(string source, out string generated)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "GeneratorTests",
            [syntaxTree],
            GetMetadataReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driver = CSharpGeneratorDriver.Create(new MediatorGenerator());
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);
        generated = outputCompilation.SyntaxTrees.Last().ToString();
        return (CSharpCompilation)outputCompilation;
    }

    private static IEnumerable<MetadataReference> GetMetadataReferences()
    {
        var trustedAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);

        foreach (var assembly in trustedAssemblies)
        {
            yield return MetadataReference.CreateFromFile(assembly);
        }

        yield return MetadataReference.CreateFromFile(typeof(Vortex.Mediator.Abstractions.IMediator).Assembly.Location);
        yield return MetadataReference.CreateFromFile(typeof(Vortex.Mediator.Abstractions.IRequest).Assembly.Location);
    }
}
