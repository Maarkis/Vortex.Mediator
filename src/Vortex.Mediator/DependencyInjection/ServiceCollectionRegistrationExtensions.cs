using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Vortex.Mediator.DependencyInjection;

internal static class ServiceCollectionRegistrationExtensions
{
    private static readonly Type[] SupportedOpenGenericTypes =
    [
        typeof(Vortex.Mediator.Abstractions.IRequestHandler<,>),
        typeof(Vortex.Mediator.Abstractions.IRequestHandler<>),
        typeof(Vortex.Mediator.Abstractions.INotificationHandler<>),
        typeof(Vortex.Mediator.Abstractions.IStreamRequestHandler<,>),
        typeof(Vortex.Mediator.Abstractions.IPipelineBehavior<,>),
        typeof(Vortex.Mediator.Abstractions.IPipelineBehavior<>),
        typeof(Vortex.Mediator.Abstractions.IStreamPipelineBehavior<,>)
    ];

    public static IServiceCollection AddMediatorServices(this IServiceCollection services, IEnumerable<Assembly> assemblies)
    {
        var descriptors = new List<ServiceDescriptor>();

        foreach (var assembly in assemblies)
        {
            foreach (var implementationType in GetCandidateTypes(assembly))
            {
                foreach (var serviceType in implementationType.GetInterfaces())
                {
                    if (!serviceType.IsGenericType)
                    {
                        continue;
                    }

                    var serviceDefinition = serviceType.GetGenericTypeDefinition();
                    if (!SupportedOpenGenericTypes.Contains(serviceDefinition))
                    {
                        continue;
                    }

                    descriptors.Add(ServiceDescriptor.Scoped(serviceType, implementationType));
                }
            }
        }

        foreach (var descriptor in descriptors
                     .DistinctBy(static descriptor => (descriptor.ServiceType, descriptor.ImplementationType)))
        {
            services.TryAddEnumerable(descriptor);
        }

        return services;
    }

    private static IEnumerable<Type> GetCandidateTypes(Assembly assembly)
    {
        Type[] types;

        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            types = exception.Types.Where(static type => type is not null).Cast<Type>().ToArray();
        }

        return types.Where(static type => type is { IsClass: true, IsAbstract: false } && !type.ContainsGenericParameters);
    }
}
