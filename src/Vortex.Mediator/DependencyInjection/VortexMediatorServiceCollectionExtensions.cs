using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Vortex.Mediator.Abstractions;

namespace Vortex.Mediator.DependencyInjection;

/// <summary>
/// Provides dependency injection registration helpers for the mediator runtime.
/// </summary>
public static class VortexMediatorServiceCollectionExtensions
{
    /// <summary>
    /// Registers the mediator runtime and scans the provided assemblies for handlers and behaviors.
    /// When no assemblies are provided, all currently loaded assemblies are scanned.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="assemblies">The assemblies to scan for mediator services.</param>
    /// <returns>The same service collection instance for chaining.</returns>
    public static IServiceCollection AddVortexMediator(this IServiceCollection services, params Assembly[] assemblies)
    {
        var assembliesToScan = assemblies.Length == 0
            ? AppDomain.CurrentDomain.GetAssemblies()
            : assemblies;

        return AddVortexMediator(services, (IEnumerable<Assembly>)assembliesToScan);
    }

    /// <summary>
    /// Registers the mediator runtime and scans the provided assemblies for handlers and behaviors.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="assemblies">The assemblies to scan for mediator services.</param>
    /// <returns>The same service collection instance for chaining.</returns>
    public static IServiceCollection AddVortexMediator(this IServiceCollection services, IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        services.AddScoped<IMediator, Mediator>();
        services.AddMediatorServices(assemblies);

        return services;
    }
}
