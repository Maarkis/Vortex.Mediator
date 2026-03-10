using Microsoft.Extensions.DependencyInjection;
using Vortex.Mediator.Abstractions;

namespace Vortex.Mediator;

public static class VortexMediatorServiceCollectionExtensions
{
    public static IServiceCollection AddVortexMediator(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IMediator, Mediator>();
        return services;
    }
}