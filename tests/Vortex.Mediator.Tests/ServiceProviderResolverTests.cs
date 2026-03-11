using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Vortex.Mediator.Internal;

namespace Vortex.Mediator.Tests;

public sealed class ServiceProviderResolverTests
{
    [Test]
    public void GetRequiredServiceReturnsRegisteredInstance()
    {
        var expected = new SampleService();
        using var provider = new ServiceCollection()
            .AddSingleton(expected)
            .BuildServiceProvider();

        var service = ServiceProviderResolver.GetRequiredService<SampleService>(provider);

        Assert.That(service, Is.SameAs(expected));
    }

    [Test]
    public void GetRequiredServiceThrowsWhenServiceIsMissing()
    {
        using var provider = new ServiceCollection().BuildServiceProvider();
        var act = () => ServiceProviderResolver.GetRequiredService<SampleService>(provider);

        Assert.That(act, Throws.InvalidOperationException);
    }

    [Test]
    public void GetRequiredServiceThrowsWhenProviderIsNull()
    {
        var act = () => ServiceProviderResolver.GetRequiredService<SampleService>(null!);

        Assert.That(act, Throws.ArgumentNullException);
    }

    [Test]
    public void GetServicesReturnsEmptyCollectionWhenThereAreNoServices()
    {
        using var provider = new ServiceCollection().BuildServiceProvider();
        var services = ServiceProviderResolver.GetServices<SampleService>(provider);

        Assert.That(services, Is.Empty);
    }

    [Test]
    public void GetServicesThrowsWhenProviderIsNull()
    {
        var act = () => ServiceProviderResolver.GetServices<SampleService>(null!);

        Assert.That(act, Throws.ArgumentNullException);
    }

    [Test]
    public void GetServicesReturnsRegisteredServices()
    {
        using var provider = new ServiceCollection()
            .AddSingleton<SampleService>()
            .AddSingleton<SampleService>()
            .BuildServiceProvider();

        var services = ServiceProviderResolver.GetServices<SampleService>(provider);

        Assert.That(services.Count, Is.EqualTo(2));
    }

    [Test]
    public void GetServicesReturnsArrayWithoutCopy()
    {
        var expected = new[] { new SampleService() };
        var provider = new DelegateServiceProvider(_ => expected);

        var services = ServiceProviderResolver.GetServices<SampleService>(provider);

        Assert.That(services, Is.SameAs(expected));
    }

    [Test]
    public void GetServicesReturnsReadOnlyListWithoutCopy()
    {
        IReadOnlyList<SampleService> expected = new List<SampleService> { new() };
        var provider = new DelegateServiceProvider(_ => expected);

        var services = ServiceProviderResolver.GetServices<SampleService>(provider);

        Assert.That(services, Is.SameAs(expected));
    }

    [Test]
    public void GetServicesMaterializesEnumerable()
    {
        IEnumerable<SampleService> expected = Yield();
        var provider = new DelegateServiceProvider(_ => expected);

        var services = ServiceProviderResolver.GetServices<SampleService>(provider);

        Assert.That(services, Is.TypeOf<SampleService[]>());
    }

    private static IEnumerable<SampleService> Yield()
    {
        yield return new SampleService();
    }

    public sealed class SampleService;

    private sealed class DelegateServiceProvider(Func<Type, object?> resolver) : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            return resolver(serviceType);
        }
    }
}
