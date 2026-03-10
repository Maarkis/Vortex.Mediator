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
    private const string RequestHandlerMetadataName = "Vortex.Mediator.Abstractions.IRequestHandler`2";
    private const string CommandHandlerMetadataName = "Vortex.Mediator.Abstractions.IRequestHandler`1";
    private const string StreamHandlerMetadataName = "Vortex.Mediator.Abstractions.IStreamRequestHandler`2";
    private const string NotificationHandlerMetadataName = "Vortex.Mediator.Abstractions.INotificationHandler`1";
    private const string RequestPipelineBehaviorMetadataName = "Vortex.Mediator.Abstractions.IPipelineBehavior`2";
    private const string CommandPipelineBehaviorMetadataName = "Vortex.Mediator.Abstractions.IPipelineBehavior`1";
    private const string StreamPipelineBehaviorMetadataName = "Vortex.Mediator.Abstractions.IStreamPipelineBehavior`2";
    private const string TaskMetadataName = "System.Threading.Tasks.Task";
    private const string TaskOfTMetadataName = "System.Threading.Tasks.Task`1";
    private const string AsyncEnumerableMetadataName = "System.Collections.Generic.IAsyncEnumerable`1";
    private const string CancellationTokenMetadataName = "System.Threading.CancellationToken";
    private const string RequestHandlerDelegateMetadataName = "Vortex.Mediator.Abstractions.RequestHandlerDelegate";
    private const string RequestHandlerDelegateOfTMetadataName = "Vortex.Mediator.Abstractions.RequestHandlerDelegate`1";
    private const string StreamHandlerDelegateMetadataName = "Vortex.Mediator.Abstractions.StreamHandlerDelegate`1";

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
        var taskType = compilation.GetTypeByMetadataName(TaskMetadataName);
        var taskOfTType = compilation.GetTypeByMetadataName(TaskOfTMetadataName);
        var asyncEnumerableType = compilation.GetTypeByMetadataName(AsyncEnumerableMetadataName);
        var requestHandlerInterface = compilation.GetTypeByMetadataName(RequestHandlerMetadataName);
        var commandHandlerInterface = compilation.GetTypeByMetadataName(CommandHandlerMetadataName);
        var streamHandlerInterface = compilation.GetTypeByMetadataName(StreamHandlerMetadataName);
        var notificationHandlerInterface = compilation.GetTypeByMetadataName(NotificationHandlerMetadataName);
        var requestPipelineBehaviorInterface = compilation.GetTypeByMetadataName(RequestPipelineBehaviorMetadataName);
        var commandPipelineBehaviorInterface = compilation.GetTypeByMetadataName(CommandPipelineBehaviorMetadataName);
        var streamPipelineBehaviorInterface = compilation.GetTypeByMetadataName(StreamPipelineBehaviorMetadataName);

        if (requestInterface is null ||
            requestWithResponseInterface is null ||
            streamRequestInterface is null ||
            notificationInterface is null ||
            taskType is null ||
            taskOfTType is null ||
            asyncEnumerableType is null ||
            requestHandlerInterface is null ||
            commandHandlerInterface is null ||
            streamHandlerInterface is null ||
            notificationHandlerInterface is null ||
            requestPipelineBehaviorInterface is null ||
            commandPipelineBehaviorInterface is null ||
            streamPipelineBehaviorInterface is null)
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

            foreach (var method in type.GetMembers("Handle").OfType<IMethodSymbol>())
            {
                var requestMethod = TryCreateRequestMethodModel(
                    method,
                    requestWithResponseInterface,
                    requestHandlerInterface,
                    requestPipelineBehaviorInterface,
                    taskOfTType);
                if (requestMethod is not null)
                {
                    requests.Add(requestMethod);
                }

                var commandMethod = TryCreateCommandMethodModel(
                    method,
                    requestInterface,
                    commandHandlerInterface,
                    commandPipelineBehaviorInterface,
                    taskType);
                if (commandMethod is not null)
                {
                    commands.Add(commandMethod);
                }

                var streamMethod = TryCreateStreamMethodModel(
                    method,
                    streamRequestInterface,
                    streamHandlerInterface,
                    streamPipelineBehaviorInterface,
                    asyncEnumerableType);
                if (streamMethod is not null)
                {
                    streams.Add(streamMethod);
                }

                var notificationMethod = TryCreateNotificationMethodModel(
                    method,
                    notificationInterface,
                    notificationHandlerInterface,
                    taskType);
                if (notificationMethod is not null)
                {
                    notifications.Add(notificationMethod);
                }
            }
        }

        return new GenerationModel(
            MergeRequests(requests.ToImmutable()),
            MergeCommands(commands.ToImmutable()),
            MergeStreams(streams.ToImmutable()),
            MergeNotifications(notifications.ToImmutable()));
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

    private static RequestModel? TryCreateRequestMethodModel(
        IMethodSymbol method,
        INamedTypeSymbol requestInterface,
        INamedTypeSymbol requestHandlerInterface,
        INamedTypeSymbol requestPipelineBehaviorInterface,
        INamedTypeSymbol taskOfTType)
    {
        if (!IsSupportedHandleMethod(method) ||
            ImplementsOpenGeneric(method.ContainingType, requestHandlerInterface) ||
            ImplementsOpenGeneric(method.ContainingType, requestPipelineBehaviorInterface) ||
            method.Parameters.Length == 0 ||
            method.ReturnType is not INamedTypeSymbol returnType ||
            !SymbolEqualityComparer.Default.Equals(returnType.OriginalDefinition, taskOfTType) ||
            method.Parameters[0].Type is not INamedTypeSymbol requestType)
        {
            return null;
        }

        foreach (var candidate in requestType.AllInterfaces)
        {
            if (!SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, requestInterface) ||
                !SymbolEqualityComparer.Default.Equals(candidate.TypeArguments[0], returnType.TypeArguments[0]))
            {
                continue;
            }

            return new RequestModel(
                GetDisplayName(requestType),
                GetDisplayName(returnType.TypeArguments[0]),
                GetDisplayName(method.ContainingType),
                GetDisplayName(method.ReturnType),
                GetHandlerParameters(method),
                GetGeneratedTypeName("MethodRequestHandler", method.ContainingType, requestType),
                method.IsStatic);
        }

        return null;
    }

    private static CommandModel? TryCreateCommandMethodModel(
        IMethodSymbol method,
        INamedTypeSymbol requestInterface,
        INamedTypeSymbol commandHandlerInterface,
        INamedTypeSymbol commandPipelineBehaviorInterface,
        INamedTypeSymbol taskType)
    {
        if (!IsSupportedHandleMethod(method) ||
            ImplementsOpenGeneric(method.ContainingType, commandHandlerInterface) ||
            ImplementsOpenGeneric(method.ContainingType, commandPipelineBehaviorInterface) ||
            method.Parameters.Length == 0 ||
            !SymbolEqualityComparer.Default.Equals(method.ReturnType, taskType) ||
            method.Parameters[0].Type is not INamedTypeSymbol requestType)
        {
            return null;
        }

        foreach (var candidate in requestType.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(candidate, requestInterface))
            {
                return new CommandModel(
                    GetDisplayName(requestType),
                    GetDisplayName(method.ContainingType),
                    GetDisplayName(method.ReturnType),
                    GetHandlerParameters(method),
                    GetGeneratedTypeName("MethodCommandHandler", method.ContainingType, requestType),
                    method.IsStatic);
            }
        }

        return null;
    }

    private static StreamRequestModel? TryCreateStreamMethodModel(IMethodSymbol method,
        INamedTypeSymbol streamRequestInterface,
        INamedTypeSymbol streamHandlerInterface,
        INamedTypeSymbol streamPipelineBehaviorInterface,
        INamedTypeSymbol asyncEnumerableType)
    {
        if (!IsSupportedHandleMethod(method) ||
            ImplementsOpenGeneric(method.ContainingType, streamHandlerInterface) ||
            ImplementsOpenGeneric(method.ContainingType, streamPipelineBehaviorInterface) ||
            method.Parameters.Length == 0 ||
            method.ReturnType is not INamedTypeSymbol returnType ||
            !SymbolEqualityComparer.Default.Equals(returnType.OriginalDefinition, asyncEnumerableType) ||
            method.Parameters[0].Type is not INamedTypeSymbol requestType)
        {
            return null;
        }

        foreach (var candidate in requestType.AllInterfaces)
        {
            if (!SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, streamRequestInterface) ||
                !SymbolEqualityComparer.Default.Equals(candidate.TypeArguments[0], returnType.TypeArguments[0]))
            {
                continue;
            }

            return new StreamRequestModel(
                GetDisplayName(requestType),
                GetDisplayName(returnType.TypeArguments[0]),
                GetDisplayName(method.ContainingType),
                GetDisplayName(method.ReturnType),
                GetHandlerParameters(method),
                GetGeneratedTypeName("MethodStreamHandler", method.ContainingType, requestType),
                method.IsStatic);
        }

        return null;
    }

    private static NotificationModel? TryCreateNotificationMethodModel(IMethodSymbol method,
        INamedTypeSymbol notificationInterface,
        INamedTypeSymbol notificationHandlerInterface,
        INamedTypeSymbol taskType)
    {
        if (!IsSupportedHandleMethod(method) ||
            ImplementsOpenGeneric(method.ContainingType, notificationHandlerInterface) ||
            method.Parameters.Length == 0 ||
            !SymbolEqualityComparer.Default.Equals(method.ReturnType, taskType) ||
            method.Parameters[0].Type is not INamedTypeSymbol notificationType)
        {
            return null;
        }

        foreach (var candidate in notificationType.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(candidate, notificationInterface))
            {
                return new NotificationModel(
                    GetDisplayName(notificationType),
                    ImmutableArray.Create(new MethodHandlerModel(
                        GetDisplayName(method.ContainingType),
                        GetDisplayName(method.ReturnType),
                        GetHandlerParameters(method),
                        GetGeneratedTypeName("MethodNotificationHandler", method.ContainingType, notificationType),
                        method.IsStatic)));
            }
        }

        return null;
    }

    private static bool IsSupportedHandleMethod(IMethodSymbol method)
    {
        return method is
        {
            MethodKind: MethodKind.Ordinary,
            IsGenericMethod: false
        } &&
        method.DeclaredAccessibility == Accessibility.Public &&
        method.ContainingType is { TypeKind: TypeKind.Class, IsAbstract: false, IsGenericType: false } &&
        !HasPipelineDelegateParameter(method) &&
        IsAccessibleFromGeneratedCode(method.ContainingType);
    }

    private static bool IsAccessibleFromGeneratedCode(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.ContainingType)
        {
            if (current.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ImplementsOpenGeneric(INamedTypeSymbol type, INamedTypeSymbol openGenericInterface)
    {
        foreach (var candidate in type.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, openGenericInterface) ||
                SymbolEqualityComparer.Default.Equals(candidate, openGenericInterface))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasPipelineDelegateParameter(IMethodSymbol method)
    {
        foreach (var parameter in method.Parameters)
        {
            var metadataName = parameter.Type switch
            {
                INamedTypeSymbol namedType when namedType.IsGenericType => namedType.OriginalDefinition.ToDisplayString(),
                _ => parameter.Type.ToDisplayString()
            };

            if (metadataName is RequestHandlerDelegateMetadataName or
                RequestHandlerDelegateOfTMetadataName or
                StreamHandlerDelegateMetadataName)
            {
                return true;
            }
        }

        return false;
    }

    private static ImmutableArray<HandlerParameterModel> GetHandlerParameters(IMethodSymbol method)
    {
        if (method.Parameters.Length <= 1)
        {
            return ImmutableArray<HandlerParameterModel>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<HandlerParameterModel>(method.Parameters.Length - 1);

        for (var index = 1; index < method.Parameters.Length; index++)
        {
            var parameter = method.Parameters[index];
            builder.Add(new HandlerParameterModel(
                GetDisplayName(parameter.Type),
                parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
                $"global::{CancellationTokenMetadataName}"
                    ? HandlerParameterKind.CancellationToken
                    : HandlerParameterKind.Service));
        }

        return builder.MoveToImmutable();
    }

    private static string GetGeneratedTypeName(string prefix, INamedTypeSymbol handlerType, INamedTypeSymbol messageType)
    {
        var name = (handlerType.ToDisplayString(TypeDisplayFormat) + "_" +
                    messageType.ToDisplayString(TypeDisplayFormat))
            .Replace('.', '_')
            .Replace(':', '_')
            .Replace('<', '_')
            .Replace('>', '_')
            .Replace(',', '_')
            .Replace('?', '_')
            .Replace(' ', '_');

        return $"{prefix}_{name}";
    }

    private static ImmutableArray<RequestModel> MergeRequests(ImmutableArray<RequestModel> requests)
    {
        var map = new Dictionary<string, RequestModel>(StringComparer.Ordinal);

        foreach (var request in requests)
        {
            if (!map.TryGetValue(request.RequestType, out var existing) || Prefer(request, existing))
            {
                map[request.RequestType] = request;
            }
        }

        return map.Values.ToImmutableArray().Sort(static (left, right) =>
            StringComparer.Ordinal.Compare(left.RequestType, right.RequestType));
    }

    private static ImmutableArray<CommandModel> MergeCommands(ImmutableArray<CommandModel> commands)
    {
        var map = new Dictionary<string, CommandModel>(StringComparer.Ordinal);

        foreach (var command in commands)
        {
            if (!map.TryGetValue(command.RequestType, out var existing) || Prefer(command, existing))
            {
                map[command.RequestType] = command;
            }
        }

        return map.Values.ToImmutableArray().Sort(static (left, right) =>
            StringComparer.Ordinal.Compare(left.RequestType, right.RequestType));
    }

    private static ImmutableArray<StreamRequestModel> MergeStreams(ImmutableArray<StreamRequestModel> streams)
    {
        var map = new Dictionary<string, StreamRequestModel>(StringComparer.Ordinal);

        foreach (var stream in streams)
        {
            if (!map.TryGetValue(stream.RequestType, out var existing) || Prefer(stream, existing))
            {
                map[stream.RequestType] = stream;
            }
        }

        return map.Values.ToImmutableArray().Sort(static (left, right) =>
            StringComparer.Ordinal.Compare(left.RequestType, right.RequestType));
    }

    private static ImmutableArray<NotificationModel> MergeNotifications(ImmutableArray<NotificationModel> notifications)
    {
        var map = new Dictionary<string, NotificationModel>(StringComparer.Ordinal);

        foreach (var notification in notifications)
        {
            if (!map.TryGetValue(notification.NotificationType, out var existing))
            {
                map[notification.NotificationType] = notification;
                continue;
            }

            map[notification.NotificationType] = existing.Append(notification.MethodHandlers);
        }

        return map.Values.ToImmutableArray().Sort(static (left, right) =>
            StringComparer.Ordinal.Compare(left.NotificationType, right.NotificationType));
    }

    private static bool Prefer(RequestModel candidate, RequestModel existing) => candidate.IsMethodHandler && !existing.IsMethodHandler;

    private static bool Prefer(CommandModel candidate, CommandModel existing) => candidate.IsMethodHandler && !existing.IsMethodHandler;

    private static bool Prefer(StreamRequestModel candidate, StreamRequestModel existing) => candidate.IsMethodHandler && !existing.IsMethodHandler;

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
        builder.AppendLine("using Microsoft.Extensions.DependencyInjection;");
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
        RenderMethodAdapters(builder, model);
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
            if (request.IsMethodHandler)
            {
                builder.Append("        var handler = new ").Append(request.GeneratedHandlerTypeName)
                    .AppendLine("(provider);");
            }
            else
            {
                builder.Append("        var handler = GetRequiredService<IRequestHandler<")
                    .Append(request.RequestType)
                    .Append(", ")
                    .Append(request.ResponseType)
                    .AppendLine(">>(provider);");
            }
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
            if (command.IsMethodHandler)
            {
                builder.Append("        var handler = new ").Append(command.GeneratedHandlerTypeName)
                    .AppendLine("(provider);");
            }
            else
            {
                builder.Append("        var handler = GetRequiredService<IRequestHandler<")
                    .Append(command.RequestType)
                    .AppendLine(">>(provider);");
            }
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
            if (stream.IsMethodHandler)
            {
                builder.Append("        var handler = new ").Append(stream.GeneratedHandlerTypeName)
                    .AppendLine("(provider);");
            }
            else
            {
                builder.Append("        var handler = GetRequiredService<IStreamRequestHandler<")
                    .Append(stream.RequestType)
                    .Append(", ")
                    .Append(stream.ResponseType)
                    .AppendLine(">>(provider);");
            }
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
            builder.Append("        var staticHandlerCount = ").Append(notification.MethodHandlers.Length).AppendLine(";");
            builder.AppendLine("        var totalHandlerCount = handlers.Count + staticHandlerCount;");
            builder.AppendLine("        return totalHandlerCount switch");
            builder.AppendLine("        {");
            builder.AppendLine("            0 => Task.CompletedTask,");
            if (notification.MethodHandlers.Length == 1)
            {
                builder.AppendLine("            1 => handlers.Count == 1");
                builder.AppendLine("                ? handlers[0].Handle(notification, cancellationToken)");
                builder.Append("                : ").Append(notification.MethodHandlers[0].GeneratedHandlerTypeName)
                    .AppendLine(".Invoke(notification, provider, cancellationToken),");
            }
            else
            {
                builder.AppendLine("            1 => handlers[0].Handle(notification, cancellationToken),");
            }
            builder.AppendLine("            _ => PublishMany(notification, provider, cancellationToken, handlers)");
            builder.AppendLine("        };");
            builder.AppendLine("    }");
            builder.AppendLine();
            builder.Append("    private static Task PublishMany(").Append(notification.NotificationType)
                .AppendLine(" notification,");
            builder.AppendLine("        IServiceProvider provider,");
            builder.AppendLine("        CancellationToken cancellationToken,");
            builder.Append("        IReadOnlyList<INotificationHandler<").Append(notification.NotificationType)
                .AppendLine(">> handlers)");
            builder.AppendLine("    {");
            builder.Append("        var tasks = new Task[handlers.Count + ").Append(notification.MethodHandlers.Length)
                .AppendLine("];");
            builder.AppendLine("        var index = 0;");
            builder.AppendLine();
            builder.AppendLine("        for (var handlerIndex = 0; handlerIndex < handlers.Count; handlerIndex++)");
            builder.AppendLine("        {");
            builder.AppendLine("            tasks[index++] = handlers[handlerIndex].Handle(notification, cancellationToken);");
            builder.AppendLine("        }");
            foreach (var methodHandler in notification.MethodHandlers)
            {
                builder.Append("        tasks[index++] = ").Append(methodHandler.GeneratedHandlerTypeName)
                    .AppendLine(".Invoke(notification, provider, cancellationToken);");
            }
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
        builder.AppendLine("            return behavior.Handle(request, next, cancellationToken);");
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
        builder.AppendLine("            return behavior.Handle(request, next, cancellationToken);");
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
        builder.AppendLine("            return behavior.Handle(request, next, cancellationToken);");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
    }

    private static void RenderMethodAdapters(StringBuilder builder, GenerationModel model)
    {
        foreach (var request in model.Requests.Where(static request => request.IsMethodHandler))
        {
            builder.Append("    private sealed class ").Append(request.GeneratedHandlerTypeName)
                .Append(" : IRequestHandler<").Append(request.RequestType).Append(", ")
                .Append(request.ResponseType).AppendLine(">");
            builder.AppendLine("    {");
            builder.AppendLine("        private readonly IServiceProvider provider;");
            builder.AppendLine();
            builder.Append("        public ").Append(request.GeneratedHandlerTypeName)
                .AppendLine("(IServiceProvider provider)");
            builder.AppendLine("        {");
            builder.AppendLine("            this.provider = provider;");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.Append("        public ").Append(request.ReturnType).Append(" Handle(")
                .Append(request.RequestType).AppendLine(" request, CancellationToken cancellationToken)");
            builder.AppendLine("        {");
            if (request.IsStaticHandler)
            {
                builder.Append("            return ").Append(request.HandlerType).Append(".Handle(request");
            }
            else
            {
                builder.Append("            return GetOrCreateInstance<").Append(request.HandlerType)
                    .Append(">(provider).Handle(request");
            }
            AppendHandlerArguments(builder, request.Parameters);
            builder.AppendLine(");");
            builder.AppendLine("        }");
            builder.AppendLine("    }");
            builder.AppendLine();
        }

        foreach (var command in model.Commands.Where(static command => command.IsMethodHandler))
        {
            builder.Append("    private sealed class ").Append(command.GeneratedHandlerTypeName)
                .Append(" : IRequestHandler<").Append(command.RequestType).AppendLine(">");
            builder.AppendLine("    {");
            builder.AppendLine("        private readonly IServiceProvider provider;");
            builder.AppendLine();
            builder.Append("        public ").Append(command.GeneratedHandlerTypeName)
                .AppendLine("(IServiceProvider provider)");
            builder.AppendLine("        {");
            builder.AppendLine("            this.provider = provider;");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.Append("        public ").Append(command.ReturnType).Append(" Handle(")
                .Append(command.RequestType).AppendLine(" request, CancellationToken cancellationToken)");
            builder.AppendLine("        {");
            if (command.IsStaticHandler)
            {
                builder.Append("            return ").Append(command.HandlerType).Append(".Handle(request");
            }
            else
            {
                builder.Append("            return GetOrCreateInstance<").Append(command.HandlerType)
                    .Append(">(provider).Handle(request");
            }
            AppendHandlerArguments(builder, command.Parameters);
            builder.AppendLine(");");
            builder.AppendLine("        }");
            builder.AppendLine("    }");
            builder.AppendLine();
        }

        foreach (var stream in model.Streams.Where(static stream => stream.IsMethodHandler))
        {
            builder.Append("    private sealed class ").Append(stream.GeneratedHandlerTypeName)
                .Append(" : IStreamRequestHandler<").Append(stream.RequestType).Append(", ")
                .Append(stream.ResponseType).AppendLine(">");
            builder.AppendLine("    {");
            builder.AppendLine("        private readonly IServiceProvider provider;");
            builder.AppendLine();
            builder.Append("        public ").Append(stream.GeneratedHandlerTypeName)
                .AppendLine("(IServiceProvider provider)");
            builder.AppendLine("        {");
            builder.AppendLine("            this.provider = provider;");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.Append("        public ").Append(stream.ReturnType).Append(" Handle(")
                .Append(stream.RequestType).AppendLine(" request, CancellationToken cancellationToken)");
            builder.AppendLine("        {");
            if (stream.IsStaticHandler)
            {
                builder.Append("            return ").Append(stream.HandlerType).Append(".Handle(request");
            }
            else
            {
                builder.Append("            return GetOrCreateInstance<").Append(stream.HandlerType)
                    .Append(">(provider).Handle(request");
            }
            AppendHandlerArguments(builder, stream.Parameters);
            builder.AppendLine(");");
            builder.AppendLine("        }");
            builder.AppendLine("    }");
            builder.AppendLine();
        }

        foreach (var notification in model.Notifications)
        {
            foreach (var methodHandler in notification.MethodHandlers)
            {
                builder.Append("    private static class ").Append(methodHandler.GeneratedHandlerTypeName).AppendLine();
                builder.AppendLine("    {");
                builder.Append("        public static ").Append(methodHandler.ReturnType).Append(" Invoke(")
                    .Append(notification.NotificationType)
                    .AppendLine(" notification, IServiceProvider provider, CancellationToken cancellationToken)");
                builder.AppendLine("        {");
                if (methodHandler.IsStaticHandler)
                {
                    builder.Append("            return ").Append(methodHandler.HandlerType).Append(".Handle(notification");
                }
                else
                {
                    builder.Append("            return GetOrCreateInstance<").Append(methodHandler.HandlerType)
                        .Append(">(provider).Handle(notification");
                }
                AppendHandlerArguments(builder, methodHandler.Parameters);
                builder.AppendLine(");");
                builder.AppendLine("        }");
                builder.AppendLine("    }");
                builder.AppendLine();
            }
        }
    }

    private static void AppendHandlerArguments(StringBuilder builder, ImmutableArray<HandlerParameterModel> parameters)
    {
        foreach (var parameter in parameters)
        {
            builder.Append(", ");
            if (parameter.Kind == HandlerParameterKind.CancellationToken)
            {
                builder.Append("cancellationToken");
                continue;
            }

            builder.Append("GetRequiredService<").Append(parameter.Type).Append(">(provider)");
        }
    }

    private static void RenderResolverHelpers(StringBuilder builder)
    {
        builder.AppendLine("    private static T GetOrCreateInstance<T>(IServiceProvider provider)");
        builder.AppendLine("        where T : notnull");
        builder.AppendLine("    {");
        builder.AppendLine("        var service = provider.GetService(typeof(T));");
        builder.AppendLine("        return service is T typed ? typed : ActivatorUtilities.CreateInstance<T>(provider);");
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
        public RequestModel(
            string requestType,
            string responseType,
            string? handlerType = null,
            string? returnType = null,
            ImmutableArray<HandlerParameterModel> parameters = default,
            string? generatedHandlerTypeName = null,
            bool isStaticHandler = false)
        {
            RequestType = requestType;
            ResponseType = responseType;
            HandlerType = handlerType;
            ReturnType = returnType ?? $"Task<{responseType}>";
            Parameters = parameters.IsDefault ? ImmutableArray<HandlerParameterModel>.Empty : parameters;
            GeneratedHandlerTypeName = generatedHandlerTypeName ?? string.Empty;
            IsStaticHandler = isStaticHandler;
        }

        public string RequestType { get; }

        public string ResponseType { get; }

        public string? HandlerType { get; }

        public string ReturnType { get; }

        public ImmutableArray<HandlerParameterModel> Parameters { get; }

        public string GeneratedHandlerTypeName { get; }

        public bool IsMethodHandler => HandlerType is not null;

        public bool IsStaticHandler { get; }
    }

    private sealed class CommandModel
    {
        public CommandModel(
            string requestType,
            string? handlerType = null,
            string? returnType = null,
            ImmutableArray<HandlerParameterModel> parameters = default,
            string? generatedHandlerTypeName = null,
            bool isStaticHandler = false)
        {
            RequestType = requestType;
            HandlerType = handlerType;
            ReturnType = returnType ?? "Task";
            Parameters = parameters.IsDefault ? ImmutableArray<HandlerParameterModel>.Empty : parameters;
            GeneratedHandlerTypeName = generatedHandlerTypeName ?? string.Empty;
            IsStaticHandler = isStaticHandler;
        }

        public string RequestType { get; }

        public string? HandlerType { get; }

        public string ReturnType { get; }

        public ImmutableArray<HandlerParameterModel> Parameters { get; }

        public string GeneratedHandlerTypeName { get; }

        public bool IsMethodHandler => HandlerType is not null;

        public bool IsStaticHandler { get; }
    }

    private sealed class StreamRequestModel
    {
        public StreamRequestModel(
            string requestType,
            string responseType,
            string? handlerType = null,
            string? returnType = null,
            ImmutableArray<HandlerParameterModel> parameters = default,
            string? generatedHandlerTypeName = null,
            bool isStaticHandler = false)
        {
            RequestType = requestType;
            ResponseType = responseType;
            HandlerType = handlerType;
            ReturnType = returnType ?? $"IAsyncEnumerable<{responseType}>";
            Parameters = parameters.IsDefault ? ImmutableArray<HandlerParameterModel>.Empty : parameters;
            GeneratedHandlerTypeName = generatedHandlerTypeName ?? string.Empty;
            IsStaticHandler = isStaticHandler;
        }

        public string RequestType { get; }

        public string ResponseType { get; }

        public string? HandlerType { get; }

        public string ReturnType { get; }

        public ImmutableArray<HandlerParameterModel> Parameters { get; }

        public string GeneratedHandlerTypeName { get; }

        public bool IsMethodHandler => HandlerType is not null;

        public bool IsStaticHandler { get; }
    }

    private sealed class NotificationModel
    {
        public NotificationModel(string notificationType, ImmutableArray<MethodHandlerModel> methodHandlers = default)
        {
            NotificationType = notificationType;
            MethodHandlers = methodHandlers.IsDefault ? ImmutableArray<MethodHandlerModel>.Empty : methodHandlers;
        }

        public string NotificationType { get; }

        public ImmutableArray<MethodHandlerModel> MethodHandlers { get; }

        public NotificationModel Append(ImmutableArray<MethodHandlerModel> methodHandlers)
        {
            return new NotificationModel(NotificationType, MethodHandlers.AddRange(methodHandlers));
        }
    }

    private sealed class MethodHandlerModel
    {
        public MethodHandlerModel(
            string handlerType,
            string returnType,
            ImmutableArray<HandlerParameterModel> parameters,
            string generatedHandlerTypeName,
            bool isStaticHandler)
        {
            HandlerType = handlerType;
            ReturnType = returnType;
            Parameters = parameters;
            GeneratedHandlerTypeName = generatedHandlerTypeName;
            IsStaticHandler = isStaticHandler;
        }

        public string HandlerType { get; }

        public string ReturnType { get; }

        public ImmutableArray<HandlerParameterModel> Parameters { get; }

        public string GeneratedHandlerTypeName { get; }

        public bool IsStaticHandler { get; }
    }

    private sealed class HandlerParameterModel
    {
        public HandlerParameterModel(string type, HandlerParameterKind kind)
        {
            Type = type;
            Kind = kind;
        }

        public string Type { get; }

        public HandlerParameterKind Kind { get; }
    }

    private enum HandlerParameterKind
    {
        Service = 0,
        CancellationToken = 1
    }
}
