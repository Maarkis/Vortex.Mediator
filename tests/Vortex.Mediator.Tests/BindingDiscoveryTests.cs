using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace Vortex.Mediator.Tests;

public sealed class BindingDiscoveryTests
{
    [Test]
    public void LoadBindingsIncludesBindingsFromMultipleAssemblies()
    {
        _ = LoadDynamicAssembly("Bindings.Multiple.One", """
#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Mediator;
using Vortex.Mediator.Abstractions;

[assembly: MediatorBindingAttribute(typeof(Bindings.Multiple.One.BindingOne))]

namespace Bindings.Multiple.One;

public sealed class BindingOne : IMediatorBinding
{
    public bool TryDispatch<TResponse>(IRequest<TResponse> request, IServiceProvider provider, CancellationToken cancellationToken, out Task<TResponse>? task)
    {
        task = null;
        return false;
    }

    public bool TryDispatch(IRequest request, IServiceProvider provider, CancellationToken cancellationToken, out Task? task)
    {
        task = null;
        return false;
    }

    public bool TryCreateStream<TResponse>(IStreamRequest<TResponse> request, IServiceProvider provider, CancellationToken cancellationToken, out IAsyncEnumerable<TResponse>? stream)
    {
        stream = null;
        return false;
    }

    public bool TryPublish(INotification notification, IServiceProvider provider, CancellationToken cancellationToken, out Task? task)
    {
        task = null;
        return false;
    }
}
""");
        _ = LoadDynamicAssembly("Bindings.Multiple.Two", """
#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Mediator;
using Vortex.Mediator.Abstractions;

[assembly: MediatorBindingAttribute(typeof(Bindings.Multiple.Two.BindingTwo))]

namespace Bindings.Multiple.Two;

public sealed class BindingTwo : IMediatorBinding
{
    public bool TryDispatch<TResponse>(IRequest<TResponse> request, IServiceProvider provider, CancellationToken cancellationToken, out Task<TResponse>? task)
    {
        task = null;
        return false;
    }

    public bool TryDispatch(IRequest request, IServiceProvider provider, CancellationToken cancellationToken, out Task? task)
    {
        task = null;
        return false;
    }

    public bool TryCreateStream<TResponse>(IStreamRequest<TResponse> request, IServiceProvider provider, CancellationToken cancellationToken, out IAsyncEnumerable<TResponse>? stream)
    {
        stream = null;
        return false;
    }

    public bool TryPublish(INotification notification, IServiceProvider provider, CancellationToken cancellationToken, out Task? task)
    {
        task = null;
        return false;
    }
}
""");

        var bindings = LoadBindings();

        Assert.That(bindings.Count(binding => binding.GetType().Namespace?.StartsWith("Bindings.Multiple", StringComparison.Ordinal) == true), Is.EqualTo(2));
    }

    [Test]
    public void LoadBindingsDeduplicatesRepeatedBindingType()
    {
        _ = LoadDynamicAssembly("Bindings.Duplicated", """
#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Mediator;
using Vortex.Mediator.Abstractions;

[assembly: MediatorBindingAttribute(typeof(Bindings.Duplicated.SharedBinding))]
[assembly: MediatorBindingAttribute(typeof(Bindings.Duplicated.SharedBinding))]

namespace Bindings.Duplicated;

public sealed class SharedBinding : IMediatorBinding
{
    public bool TryDispatch<TResponse>(IRequest<TResponse> request, IServiceProvider provider, CancellationToken cancellationToken, out Task<TResponse>? task)
    {
        task = null;
        return false;
    }

    public bool TryDispatch(IRequest request, IServiceProvider provider, CancellationToken cancellationToken, out Task? task)
    {
        task = null;
        return false;
    }

    public bool TryCreateStream<TResponse>(IStreamRequest<TResponse> request, IServiceProvider provider, CancellationToken cancellationToken, out IAsyncEnumerable<TResponse>? stream)
    {
        stream = null;
        return false;
    }

    public bool TryPublish(INotification notification, IServiceProvider provider, CancellationToken cancellationToken, out Task? task)
    {
        task = null;
        return false;
    }
}
""");

        var bindings = LoadBindings();

        Assert.That(bindings.Count(binding => binding.GetType().FullName == "Bindings.Duplicated.SharedBinding"), Is.EqualTo(1));
    }

    private static IReadOnlyList<IMediatorBinding> LoadBindings()
    {
        var method = typeof(Mediator).GetMethod("LoadBindings", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (IReadOnlyList<IMediatorBinding>)method.Invoke(null, null)!;
    }

    private static Assembly LoadDynamicAssembly(string assemblyName, string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            assemblyName,
            [syntaxTree],
            GetMetadataReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        if (!result.Success)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, result.Diagnostics));
        }

        stream.Position = 0;
        return Assembly.Load(stream.ToArray());
    }

    private static IEnumerable<MetadataReference> GetMetadataReferences()
    {
        var trustedAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);

        foreach (var assembly in trustedAssemblies)
        {
            yield return MetadataReference.CreateFromFile(assembly);
        }

        yield return MetadataReference.CreateFromFile(typeof(IMediatorBinding).Assembly.Location);
        yield return MetadataReference.CreateFromFile(typeof(Vortex.Mediator.Abstractions.IRequest).Assembly.Location);
    }
}
