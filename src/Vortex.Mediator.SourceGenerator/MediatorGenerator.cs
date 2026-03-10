using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Vortex.Mediator.SourceGenerator;

[Generator]
public sealed class MediatorGenerator : IIncrementalGenerator
{
    private const string RequestMetadataName = "Vortex.Mediator.Abstractions.IRequest`1";
    private const string NotificationMetadataName = "Vortex.Mediator.Abstractions.INotification";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var compilationProvider =
            context.CompilationProvider.Select(static (compilation, _) => CreateModel(compilation));

        context.RegisterSourceOutput(compilationProvider,
            static (productionContext, model) =>
            {
                productionContext.AddSource("Mediator.Dispatch.g.cs",
                    SourceText.From(RenderSource(model), Encoding.UTF8));
            });
    }

    private static GenerationModel CreateModel(Compilation compilation)
    {
        var requestInterface = compilation.GetTypeByMetadataName(RequestMetadataName);
        var notificationInterface = compilation.GetTypeByMetadataName(NotificationMetadataName);

        if (requestInterface is null || notificationInterface is null)
        {
            return new GenerationModel(ImmutableArray<RequestModel>.Empty, ImmutableArray<NotificationModel>.Empty);
        }

        var requests = ImmutableArray.CreateBuilder<RequestModel>();
        var notifications = ImmutableArray.CreateBuilder<NotificationModel>();

        foreach (var type in EnumerateTypes(compilation.Assembly.GlobalNamespace))
        {
            if (type.TypeKind is TypeKind.Interface or TypeKind.TypeParameter)
            {
                continue;
            }

            var request = TryCreateRequestModel(type, requestInterface);
            if (request is not null)
            {
                requests.Add(request);
            }

            var notification = TryCreateNotificationModel(type, notificationInterface);
            if (notification is not null)
            {
                notifications.Add(notification);
            }
        }

        return new GenerationModel(
            requests.ToImmutable().Sort(static (left, right) =>
                StringComparer.Ordinal.Compare(left.RequestType, right.RequestType)),
            notifications.ToImmutable().Sort(static (left, right) =>
                StringComparer.Ordinal.Compare(left.NotificationType, right.NotificationType)));
    }

    private static RequestModel? TryCreateRequestModel(INamedTypeSymbol type, INamedTypeSymbol requestInterface)
    {
        foreach (var candidate in type.AllInterfaces)
        {
            if (!SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, requestInterface))
            {
                continue;
            }

            return new RequestModel(
                GetDisplayName(type),
                GetDisplayName(candidate.TypeArguments[0]));
        }

        return null;
    }

    private static NotificationModel? TryCreateNotificationModel(INamedTypeSymbol type,
        INamedTypeSymbol notificationInterface)
    {
        foreach (var candidate in type.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(candidate, notificationInterface))
            {
                return new NotificationModel(GetDisplayName(type));
            }
        }

        return null;
    }

    private static IEnumerable<INamedTypeSymbol> EnumerateTypes(INamespaceSymbol @namespace)
    {
        foreach (var type in @namespace.GetTypeMembers())
        {
            foreach (var nested in EnumerateTypes(type))
            {
                yield return nested;
            }
        }

        foreach (var child in @namespace.GetNamespaceMembers())
        {
            foreach (var nested in EnumerateTypes(child))
            {
                yield return nested;
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> EnumerateTypes(INamedTypeSymbol type)
    {
        yield return type;

        foreach (var nestedType in type.GetTypeMembers())
        {
            foreach (var nested in EnumerateTypes(nestedType))
            {
                yield return nested;
            }
        }
    }

    private static string GetDisplayName(ITypeSymbol symbol)
    {
        return symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    private static string RenderSource(GenerationModel model)
    {
        var builder = new StringBuilder();
        builder.AppendLine("#nullable enable");
        builder.AppendLine("using System;");
        builder.AppendLine("using System.Collections.Generic;");
        builder.AppendLine("using System.Linq;");
        builder.AppendLine("using System.Threading;");
        builder.AppendLine("using System.Threading.Tasks;");
        builder.AppendLine("using Vortex.Mediator;");
        builder.AppendLine("using Vortex.Mediator.Abstractions;");
        builder.AppendLine();
        builder.AppendLine("[assembly: global::Vortex.Mediator.MediatorBindingAttribute(typeof(global::Vortex.Mediator.GeneratedMediatorBinding))]");
        builder.AppendLine();
        builder.AppendLine("namespace Vortex.Mediator;");
        builder.AppendLine();
        builder.AppendLine("internal sealed class GeneratedMediatorBinding : IMediatorBinding");
        builder.AppendLine("{");
        builder.AppendLine("    public bool TryDispatch<TResponse>(");
        builder.AppendLine("        IRequest<TResponse> request,");
        builder.AppendLine("        IServiceProvider provider,");
        builder.AppendLine("        CancellationToken cancellationToken,");
        builder.AppendLine("        out Task<TResponse>? task)");
        builder.AppendLine("    {");
        builder.AppendLine("        task = request switch");
        builder.AppendLine("        {");

        if (model.Requests.Length == 0)
        {
            builder.AppendLine("            _ => null");
        }
        else
        {
            foreach (var request in model.Requests)
            {
                builder.Append("            ").Append(request.RequestType)
                    .Append(" typed => (Task<TResponse>)(object)Dispatch(typed, provider, cancellationToken),").AppendLine();
            }

            builder.AppendLine("            _ => null");
        }

        builder.AppendLine("        };");
        builder.AppendLine("        return task is not null;");
        builder.AppendLine("    }");
        builder.AppendLine();

        foreach (var request in model.Requests)
        {
            builder.Append("    private Task<").Append(request.ResponseType).Append("> Dispatch(")
                .Append(request.RequestType).AppendLine(" request,");
            builder.AppendLine("        IServiceProvider provider,");
            builder.AppendLine("        CancellationToken cancellationToken)");
            builder.AppendLine("    {");
            builder.Append("        var handler = GetRequiredService<IRequestHandler<")
                .Append(request.RequestType)
                .Append(", ")
                .Append(request.ResponseType)
                .AppendLine(">>(provider);");
            builder.AppendLine("        var behaviors = GetServices<IPipelineBehavior<" + request.RequestType + ", " + request.ResponseType + ">>(provider);");
            builder.AppendLine("        return ExecutePipeline(request, cancellationToken, handler, behaviors);");
            builder.AppendLine("    }");
            builder.AppendLine();
        }

        builder.AppendLine("    public bool TryPublish(");
        builder.AppendLine("        INotification notification,");
        builder.AppendLine("        IServiceProvider provider,");
        builder.AppendLine("        CancellationToken cancellationToken,");
        builder.AppendLine("        out Task? task)");
        builder.AppendLine("    {");
        builder.AppendLine("        task = notification switch");
        builder.AppendLine("        {");

        if (model.Notifications.Length == 0)
        {
            builder.AppendLine("            _ => Task.CompletedTask");
        }
        else
        {
            foreach (var notification in model.Notifications)
            {
                builder.Append("            ").Append(notification.NotificationType)
                    .Append(" typed => Publish(typed, provider, cancellationToken),").AppendLine();
            }

            builder.AppendLine("            _ => Task.CompletedTask");
        }

        builder.AppendLine("        };");
        builder.AppendLine("        return task is not null;");
        builder.AppendLine("    }");
        builder.AppendLine();

        foreach (var notification in model.Notifications)
        {
            builder.Append("    private Task Publish(").Append(notification.NotificationType)
                .AppendLine(" notification,");
            builder.AppendLine("        IServiceProvider provider,");
            builder.AppendLine("        CancellationToken cancellationToken)");
            builder.AppendLine("    {");
            builder.Append("        var handlers = GetServices<INotificationHandler<")
                .Append(notification.NotificationType)
                .AppendLine(">>(provider);");
            builder.AppendLine("        return handlers.Count switch");
            builder.AppendLine("        {");
            builder.AppendLine("            0 => Task.CompletedTask,");
            builder.AppendLine("            1 => handlers[0].Handle(notification, cancellationToken),");
            builder.AppendLine("            _ => PublishMany(notification, cancellationToken, handlers)");
            builder.AppendLine("        };");
            builder.AppendLine("    }");
            builder.AppendLine();
            builder.AppendLine("    private static Task PublishMany(" + notification.NotificationType + " notification,");
            builder.AppendLine("        CancellationToken cancellationToken,");
            builder.AppendLine("        IReadOnlyList<INotificationHandler<" + notification.NotificationType + ">> handlers)");
            builder.AppendLine("    {");
            builder.AppendLine("        var tasks = new Task[handlers.Count];");
            builder.AppendLine();
            builder.AppendLine("        for (var index = 0; index < handlers.Count; index++)");
            builder.AppendLine("        {");
            builder.AppendLine("            tasks[index] = handlers[index].Handle(notification, cancellationToken);");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        return Task.WhenAll(tasks);");
            builder.AppendLine("    }");
            builder.AppendLine();
        }

        builder.AppendLine("    private static Task<TResponse> ExecutePipeline<TRequest, TResponse>(");
        builder.AppendLine("        TRequest request,");
        builder.AppendLine("        CancellationToken cancellationToken,");
        builder.AppendLine("        IRequestHandler<TRequest, TResponse> handler,");
        builder.AppendLine("        IReadOnlyList<IPipelineBehavior<TRequest, TResponse>> behaviors)");
        builder.AppendLine("        where TRequest : IRequest<TResponse>");
        builder.AppendLine("    {");
        builder.AppendLine("        if (behaviors.Count == 0)");
        builder.AppendLine("        {");
        builder.AppendLine("            return handler.Handle(request, cancellationToken);");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        RequestHandlerDelegate<TResponse> next =");
        builder.AppendLine("            new HandlerInvocation<TRequest, TResponse>(request, cancellationToken, handler).Invoke;");
        builder.AppendLine();
        builder.AppendLine("        for (var index = behaviors.Count - 1; index >= 0; index--)");
        builder.AppendLine("        {");
        builder.AppendLine("            next = new BehaviorInvocation<TRequest, TResponse>(");
        builder.AppendLine("                request,");
        builder.AppendLine("                cancellationToken,");
        builder.AppendLine("                behaviors[index],");
        builder.AppendLine("                next).Invoke;");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        return next();");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private sealed class HandlerInvocation<TRequest, TResponse>(");
        builder.AppendLine("        TRequest request,");
        builder.AppendLine("        CancellationToken cancellationToken,");
        builder.AppendLine("        IRequestHandler<TRequest, TResponse> handler)");
        builder.AppendLine("        where TRequest : IRequest<TResponse>");
        builder.AppendLine("    {");
        builder.AppendLine("        public Task<TResponse> Invoke()");
        builder.AppendLine("        {");
        builder.AppendLine("            return handler.Handle(request, cancellationToken);");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private sealed class BehaviorInvocation<TRequest, TResponse>(");
        builder.AppendLine("        TRequest request,");
        builder.AppendLine("        CancellationToken cancellationToken,");
        builder.AppendLine("        IPipelineBehavior<TRequest, TResponse> behavior,");
        builder.AppendLine("        RequestHandlerDelegate<TResponse> next)");
        builder.AppendLine("    {");
        builder.AppendLine("        public Task<TResponse> Invoke()");
        builder.AppendLine("        {");
        builder.AppendLine("            return behavior.Handle(request, cancellationToken, next);");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private static T GetRequiredService<T>(IServiceProvider provider)");
        builder.AppendLine("        where T : notnull");
        builder.AppendLine("    {");
        builder.AppendLine("        return RequiredServiceCache<T>.Resolve(provider);");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private static IReadOnlyList<T> GetServices<T>(IServiceProvider provider)");
        builder.AppendLine("    {");
        builder.AppendLine("        return ServicesCache<T>.Resolve(provider);");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private static class RequiredServiceCache<T>");
        builder.AppendLine("        where T : notnull");
        builder.AppendLine("    {");
        builder.AppendLine("        public static readonly Func<IServiceProvider, T> Resolve = CreateResolver();");
        builder.AppendLine();
        builder.AppendLine("        private static Func<IServiceProvider, T> CreateResolver()");
        builder.AppendLine("        {");
        builder.AppendLine("            return static provider =>");
        builder.AppendLine("            {");
        builder.AppendLine("                var service = provider.GetService(typeof(T));");
        builder.AppendLine("                if (service is T typed)");
        builder.AppendLine("                {");
        builder.AppendLine("                    return typed;");
        builder.AppendLine("                }");
        builder.AppendLine();
        builder.AppendLine("                throw new InvalidOperationException($\"Service of type '{typeof(T)}' is not registered.\");");
        builder.AppendLine("            };");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private static class ServicesCache<T>");
        builder.AppendLine("    {");
        builder.AppendLine("        public static readonly Func<IServiceProvider, IReadOnlyList<T>> Resolve = CreateResolver();");
        builder.AppendLine();
        builder.AppendLine("        private static Func<IServiceProvider, IReadOnlyList<T>> CreateResolver()");
        builder.AppendLine("        {");
        builder.AppendLine("            return static provider =>");
        builder.AppendLine("            {");
        builder.AppendLine("                var services = provider.GetService(typeof(IEnumerable<T>));");
        builder.AppendLine();
        builder.AppendLine("                return services switch");
        builder.AppendLine("                {");
        builder.AppendLine("                    null => Array.Empty<T>(),");
        builder.AppendLine("                    T[] array => array,");
        builder.AppendLine("                    IReadOnlyList<T> readOnlyList => readOnlyList,");
        builder.AppendLine("                    IEnumerable<T> enumerable => enumerable.ToArray(),");
        builder.AppendLine("                    _ => Array.Empty<T>()");
        builder.AppendLine("                };");
        builder.AppendLine("            };");
        builder.AppendLine("        }");
        builder.AppendLine("    }");

        builder.AppendLine("}");

        return builder.ToString();
    }

    private sealed record GenerationModel(
        ImmutableArray<RequestModel> Requests,
        ImmutableArray<NotificationModel> Notifications);

    private sealed record RequestModel(
        string RequestType,
        string ResponseType);

    private sealed record NotificationModel(string NotificationType);
}
