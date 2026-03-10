using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Vortex.Mediator.Abstractions;

namespace Vortex.Mediator.DependencyInjection;

public static class VortexMediatorServiceCollectionExtensions
{
    public static IServiceCollection AddVortexMediator(this IServiceCollection services, params Assembly[] assemblies)
    {
        var assembliesToScan = assemblies.Length == 0
            ? AppDomain.CurrentDomain.GetAssemblies()
            : assemblies;

        return AddVortexMediator(services, (IEnumerable<Assembly>)assembliesToScan);
    }

    public static IServiceCollection AddVortexMediator(this IServiceCollection services, IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        services.AddScoped<IMediator, Mediator>();
        services.AddMediatorServices(assemblies);

        return services;
    }
}
