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
    private const string RequestMetadataName = "Vortex.Mediator.Abstractions.IRequest";
    private const string RequestWithResponseMetadataName = "Vortex.Mediator.Abstractions.IRequest`1";
    private const string StreamRequestMetadataName = "Vortex.Mediator.Abstractions.IStreamRequest`1";
    private const string NotificationMetadataName = "Vortex.Mediator.Abstractions.INotification";

    private static readonly SymbolDisplayFormat TypeDisplayFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions:
        SymbolDisplayGenericsOptions.IncludeTypeParameters |
        SymbolDisplayGenericsOptions.IncludeVariance,
        miscellaneousOptions:
        SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
        SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

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
        var requestWithResponseInterface = compilation.GetTypeByMetadataName(RequestWithResponseMetadataName);
        var streamRequestInterface = compilation.GetTypeByMetadataName(StreamRequestMetadataName);
        var notificationInterface = compilation.GetTypeByMetadataName(NotificationMetadataName);

        if (requestInterface is null ||
            requestWithResponseInterface is null ||
            streamRequestInterface is null ||
            notificationInterface is null)
        {
            return new GenerationModel(
                ImmutableArray<RequestModel>.Empty,
                ImmutableArray<CommandModel>.Empty,
                ImmutableArray<StreamRequestModel>.Empty,
                ImmutableArray<NotificationModel>.Empty);
        }

        var requests = ImmutableArray.CreateBuilder<RequestModel>();
        var commands = ImmutableArray.CreateBuilder<CommandModel>();
        var streams = ImmutableArray.CreateBuilder<StreamRequestModel>();
        var notifications = ImmutableArray.CreateBuilder<NotificationModel>();

        foreach (var type in EnumerateTypes(compilation.Assembly.GlobalNamespace))
        {
            if (type.TypeKind is TypeKind.Interface or TypeKind.TypeParameter)
            {
                continue;
            }

            var request = TryCreateRequestModel(type, requestWithResponseInterface);
            if (request is not null)
            {
                requests.Add(request);
            }

            var command = TryCreateCommandModel(type, requestInterface);
            if (command is not null)
            {
                commands.Add(command);
            }

            var stream = TryCreateStreamRequestModel(type, streamRequestInterface);
            if (stream is not null)
            {
                streams.Add(stream);
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
            commands.ToImmutable().Sort(static (left, right) =>
                StringComparer.Ordinal.Compare(left.RequestType, right.RequestType)),
            streams.ToImmutable().Sort(static (left, right) =>
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

    private static CommandModel? TryCreateCommandModel(INamedTypeSymbol type, INamedTypeSymbol requestInterface)
    {
        foreach (var candidate in type.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(candidate, requestInterface))
            {
                return new CommandModel(GetDisplayName(type));
            }
        }

        return null;
    }

    private static StreamRequestModel? TryCreateStreamRequestModel(INamedTypeSymbol type,
        INamedTypeSymbol streamRequestInterface)
    {
        foreach (var candidate in type.AllInterfaces)
        {
            if (!SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, streamRequestInterface))
            {
                continue;
            }

            return new StreamRequestModel(
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
        return symbol.ToDisplayString(TypeDisplayFormat);
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
        builder.AppendLine(
            "[assembly: global::Vortex.Mediator.MediatorBindingAttribute(typeof(global::Vortex.Mediator.GeneratedMediatorBinding))]");
        builder.AppendLine();
        builder.AppendLine("namespace Vortex.Mediator;");
        builder.AppendLine();
        builder.AppendLine("internal sealed class GeneratedMediatorBinding : IMediatorBinding");
        builder.AppendLine("{");
        RenderRequestDispatch(builder, model.Requests);
        builder.AppendLine();
        RenderCommandDispatch(builder, model.Commands);
        builder.AppendLine();
        RenderStreamDispatch(builder, model.Streams);
        builder.AppendLine();
        RenderNotificationDispatch(builder, model.Notifications);
        builder.AppendLine();
        RenderPipelineHelpers(builder);
        builder.AppendLine();
        RenderResolverHelpers(builder);
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static void RenderRequestDispatch(StringBuilder builder, ImmutableArray<RequestModel> requests)
    {
        builder.AppendLine("    public bool TryDispatch<TResponse>(");
        builder.AppendLine("        IRequest<TResponse> request,");
        builder.AppendLine("        IServiceProvider provider,");
        builder.AppendLine("        CancellationToken cancellationToken,");
        builder.AppendLine("        out Task<TResponse>? task)");
        builder.AppendLine("    {");
        builder.AppendLine("        task = request switch");
        builder.AppendLine("        {");

        if (requests.Length == 0)
        {
            builder.AppendLine("            _ => null");
        }
        else
        {
            foreach (var request in requests)
            {
                builder.Append("            ").Append(request.RequestType)
                    .Append(" typed => (Task<TResponse>)(object)Dispatch(typed, provider, cancellationToken),")
                    .AppendLine();
            }

            builder.AppendLine("            _ => null");
        }

        builder.AppendLine("        };");
        builder.AppendLine("        return task is not null;");
        builder.AppendLine("    }");
        builder.AppendLine();

        foreach (var request in requests)
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
            builder.Append("        var behaviors = GetServices<IPipelineBehavior<")
                .Append(request.RequestType)
                .Append(", ")
                .Append(request.ResponseType)
                .AppendLine(">>(provider);");
            builder.AppendLine("        return ExecutePipeline(request, cancellationToken, handler, behaviors);");
            builder.AppendLine("    }");
            builder.AppendLine();
        }
    }

    private static void RenderCommandDispatch(StringBuilder builder, ImmutableArray<CommandModel> commands)
    {
        builder.AppendLine("    public bool TryDispatch(");
        builder.AppendLine("        IRequest request,");
        builder.AppendLine("        IServiceProvider provider,");
        builder.AppendLine("        CancellationToken cancellationToken,");
        builder.AppendLine("        out Task? task)");
        builder.AppendLine("    {");
        builder.AppendLine("        task = request switch");
        builder.AppendLine("        {");

        if (commands.Length == 0)
        {
            builder.AppendLine("            _ => null");
        }
        else
        {
            foreach (var command in commands)
            {
                builder.Append("            ").Append(command.RequestType)
                    .Append(" typed => Dispatch(typed, provider, cancellationToken),").AppendLine();
            }

            builder.AppendLine("            _ => null");
        }

        builder.AppendLine("        };");
        builder.AppendLine("        return task is not null;");
        builder.AppendLine("    }");
        builder.AppendLine();

        foreach (var command in commands)
        {
            builder.Append("    private Task Dispatch(").Append(command.RequestType).AppendLine(" request,");
            builder.AppendLine("        IServiceProvider provider,");
            builder.AppendLine("        CancellationToken cancellationToken)");
            builder.AppendLine("    {");
            builder.Append("        var handler = GetRequiredService<IRequestHandler<")
                .Append(command.RequestType)
                .AppendLine(">>(provider);");
            builder.Append("        var behaviors = GetServices<IPipelineBehavior<")
                .Append(command.RequestType)
                .AppendLine(">>(provider);");
            builder.AppendLine("        return ExecutePipeline(request, cancellationToken, handler, behaviors);");
            builder.AppendLine("    }");
            builder.AppendLine();
        }
    }

    private static void RenderStreamDispatch(StringBuilder builder, ImmutableArray<StreamRequestModel> streams)
    {
        builder.AppendLine("    public bool TryCreateStream<TResponse>(");
        builder.AppendLine("        IStreamRequest<TResponse> request,");
        builder.AppendLine("        IServiceProvider provider,");
        builder.AppendLine("        CancellationToken cancellationToken,");
        builder.AppendLine("        out IAsyncEnumerable<TResponse>? stream)");
        builder.AppendLine("    {");
        builder.AppendLine("        stream = request switch");
        builder.AppendLine("        {");

        if (streams.Length == 0)
        {
            builder.AppendLine("            _ => null");
        }
        else
        {
            foreach (var stream in streams)
            {
                builder.Append("            ").Append(stream.RequestType)
                    .Append(
                        " typed => (IAsyncEnumerable<TResponse>)(object)CreateStream(typed, provider, cancellationToken),")
                    .AppendLine();
            }

            builder.AppendLine("            _ => null");
        }

        builder.AppendLine("        };");
        builder.AppendLine("        return stream is not null;");
        builder.AppendLine("    }");
        builder.AppendLine();

        foreach (var stream in streams)
        {
            builder.Append("    private IAsyncEnumerable<").Append(stream.ResponseType).Append("> CreateStream(")
                .Append(stream.RequestType).AppendLine(" request,");
            builder.AppendLine("        IServiceProvider provider,");
            builder.AppendLine("        CancellationToken cancellationToken)");
            builder.AppendLine("    {");
            builder.Append("        var handler = GetRequiredService<IStreamRequestHandler<")
                .Append(stream.RequestType)
                .Append(", ")
                .Append(stream.ResponseType)
                .AppendLine(">>(provider);");
            builder.Append("        var behaviors = GetServices<IStreamPipelineBehavior<")
                .Append(stream.RequestType)
                .Append(", ")
                .Append(stream.ResponseType)
                .AppendLine(">>(provider);");
            builder.AppendLine("        return ExecuteStreamPipeline(request, cancellationToken, handler, behaviors);");
            builder.AppendLine("    }");
            builder.AppendLine();
        }
    }

    private static void RenderNotificationDispatch(StringBuilder builder,
        ImmutableArray<NotificationModel> notifications)
    {
        builder.AppendLine("    public bool TryPublish(");
        builder.AppendLine("        INotification notification,");
        builder.AppendLine("        IServiceProvider provider,");
        builder.AppendLine("        CancellationToken cancellationToken,");
        builder.AppendLine("        out Task? task)");
        builder.AppendLine("    {");
        builder.AppendLine("        task = notification switch");
        builder.AppendLine("        {");

        if (notifications.Length == 0)
        {
            builder.AppendLine("            _ => Task.CompletedTask");
        }
        else
        {
            foreach (var notification in notifications)
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

        foreach (var notification in notifications)
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
            builder.Append("    private static Task PublishMany(").Append(notification.NotificationType)
                .AppendLine(" notification,");
            builder.AppendLine("        CancellationToken cancellationToken,");
            builder.Append("        IReadOnlyList<INotificationHandler<").Append(notification.NotificationType)
                .AppendLine(">> handlers)");
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
    }

    private static void RenderPipelineHelpers(StringBuilder builder)
    {
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
        builder.AppendLine(
            "            new HandlerInvocation<TRequest, TResponse>(request, cancellationToken, handler).Invoke;");
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
        builder.AppendLine("    private static Task ExecutePipeline<TRequest>(");
        builder.AppendLine("        TRequest request,");
        builder.AppendLine("        CancellationToken cancellationToken,");
        builder.AppendLine("        IRequestHandler<TRequest> handler,");
        builder.AppendLine("        IReadOnlyList<IPipelineBehavior<TRequest>> behaviors)");
        builder.AppendLine("        where TRequest : IRequest");
        builder.AppendLine("    {");
        builder.AppendLine("        if (behaviors.Count == 0)");
        builder.AppendLine("        {");
        builder.AppendLine("            return handler.Handle(request, cancellationToken);");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        RequestHandlerDelegate next =");
        builder.AppendLine(
            "            new CommandHandlerInvocation<TRequest>(request, cancellationToken, handler).Invoke;");
        builder.AppendLine();
        builder.AppendLine("        for (var index = behaviors.Count - 1; index >= 0; index--)");
        builder.AppendLine("        {");
        builder.AppendLine("            next = new CommandBehaviorInvocation<TRequest>(");
        builder.AppendLine("                request,");
        builder.AppendLine("                cancellationToken,");
        builder.AppendLine("                behaviors[index],");
        builder.AppendLine("                next).Invoke;");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        return next();");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine(
            "    private static IAsyncEnumerable<TResponse> ExecuteStreamPipeline<TRequest, TResponse>(");
        builder.AppendLine("        TRequest request,");
        builder.AppendLine("        CancellationToken cancellationToken,");
        builder.AppendLine("        IStreamRequestHandler<TRequest, TResponse> handler,");
        builder.AppendLine("        IReadOnlyList<IStreamPipelineBehavior<TRequest, TResponse>> behaviors)");
        builder.AppendLine("        where TRequest : IStreamRequest<TResponse>");
        builder.AppendLine("    {");
        builder.AppendLine("        if (behaviors.Count == 0)");
        builder.AppendLine("        {");
        builder.AppendLine("            return handler.Handle(request, cancellationToken);");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        StreamHandlerDelegate<TResponse> next =");
        builder.AppendLine(
            "            new StreamHandlerInvocation<TRequest, TResponse>(request, cancellationToken, handler).Invoke;");
        builder.AppendLine();
        builder.AppendLine("        for (var index = behaviors.Count - 1; index >= 0; index--)");
        builder.AppendLine("        {");
        builder.AppendLine("            next = new StreamBehaviorInvocation<TRequest, TResponse>(");
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
        builder.AppendLine("    private sealed class CommandHandlerInvocation<TRequest>(");
        builder.AppendLine("        TRequest request,");
        builder.AppendLine("        CancellationToken cancellationToken,");
        builder.AppendLine("        IRequestHandler<TRequest> handler)");
        builder.AppendLine("        where TRequest : IRequest");
        builder.AppendLine("    {");
        builder.AppendLine("        public Task Invoke()");
        builder.AppendLine("        {");
        builder.AppendLine("            return handler.Handle(request, cancellationToken);");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private sealed class CommandBehaviorInvocation<TRequest>(");
        builder.AppendLine("        TRequest request,");
        builder.AppendLine("        CancellationToken cancellationToken,");
        builder.AppendLine("        IPipelineBehavior<TRequest> behavior,");
        builder.AppendLine("        RequestHandlerDelegate next)");
        builder.AppendLine("        where TRequest : IRequest");
        builder.AppendLine("    {");
        builder.AppendLine("        public Task Invoke()");
        builder.AppendLine("        {");
        builder.AppendLine("            return behavior.Handle(request, cancellationToken, next);");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private sealed class StreamHandlerInvocation<TRequest, TResponse>(");
        builder.AppendLine("        TRequest request,");
        builder.AppendLine("        CancellationToken cancellationToken,");
        builder.AppendLine("        IStreamRequestHandler<TRequest, TResponse> handler)");
        builder.AppendLine("        where TRequest : IStreamRequest<TResponse>");
        builder.AppendLine("    {");
        builder.AppendLine("        public IAsyncEnumerable<TResponse> Invoke()");
        builder.AppendLine("        {");
        builder.AppendLine("            return handler.Handle(request, cancellationToken);");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private sealed class StreamBehaviorInvocation<TRequest, TResponse>(");
        builder.AppendLine("        TRequest request,");
        builder.AppendLine("        CancellationToken cancellationToken,");
        builder.AppendLine("        IStreamPipelineBehavior<TRequest, TResponse> behavior,");
        builder.AppendLine("        StreamHandlerDelegate<TResponse> next)");
        builder.AppendLine("        where TRequest : IStreamRequest<TResponse>");
        builder.AppendLine("    {");
        builder.AppendLine("        public IAsyncEnumerable<TResponse> Invoke()");
        builder.AppendLine("        {");
        builder.AppendLine("            return behavior.Handle(request, cancellationToken, next);");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
    }

    private static void RenderResolverHelpers(StringBuilder builder)
    {
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
        builder.AppendLine(
            "                throw new InvalidOperationException($\"Service of type '{typeof(T)}' is not registered.\");");
        builder.AppendLine("            };");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private static class ServicesCache<T>");
        builder.AppendLine("    {");
        builder.AppendLine(
            "        public static readonly Func<IServiceProvider, IReadOnlyList<T>> Resolve = CreateResolver();");
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
    }

    private sealed class GenerationModel
    {
        public GenerationModel(
            ImmutableArray<RequestModel> requests,
            ImmutableArray<CommandModel> commands,
            ImmutableArray<StreamRequestModel> streams,
            ImmutableArray<NotificationModel> notifications)
        {
            Requests = requests;
            Commands = commands;
            Streams = streams;
            Notifications = notifications;
        }

        public ImmutableArray<RequestModel> Requests { get; }

        public ImmutableArray<CommandModel> Commands { get; }

        public ImmutableArray<StreamRequestModel> Streams { get; }

        public ImmutableArray<NotificationModel> Notifications { get; }
    }

    private sealed class RequestModel
    {
        public RequestModel(string requestType, string responseType)
        {
            RequestType = requestType;
            ResponseType = responseType;
        }

        public string RequestType { get; }

        public string ResponseType { get; }
    }

    private sealed class CommandModel
    {
        public CommandModel(string requestType)
        {
            RequestType = requestType;
        }

        public string RequestType { get; }
    }

    private sealed class StreamRequestModel
    {
        public StreamRequestModel(string requestType, string responseType)
        {
            RequestType = requestType;
            ResponseType = responseType;
        }

        public string RequestType { get; }

        public string ResponseType { get; }
    }

    private sealed class NotificationModel
    {
        public NotificationModel(string notificationType)
        {
            NotificationType = notificationType;
        }

        public string NotificationType { get; }
    }
}
