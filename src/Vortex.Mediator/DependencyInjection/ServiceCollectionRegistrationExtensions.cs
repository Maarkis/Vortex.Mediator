using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vortex.Mediator.Abstractions;

namespace Vortex.Mediator.DependencyInjection;

internal static class ServiceCollectionRegistrationExtensions
{
    private static readonly Type[] SupportedOpenGenericTypes =
    [
        typeof(IRequestHandler<,>),
        typeof(IRequestHandler<>),
        typeof(INotificationHandler<>),
        typeof(IStreamRequestHandler<,>),
        typeof(IPipelineBehavior<,>),
        typeof(IPipelineBehavior<>),
        typeof(IStreamPipelineBehavior<,>)
    ];

    public static IServiceCollection AddMediatorServices(this IServiceCollection services,
        IEnumerable<Assembly> assemblies)
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

        return types.Where(static type =>
            type is { IsClass: true, IsAbstract: false } && !type.ContainsGenericParameters);
    }
}