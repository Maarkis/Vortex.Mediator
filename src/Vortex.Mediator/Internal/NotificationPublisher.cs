using Vortex.Mediator.Abstractions;

namespace Vortex.Mediator.Internal;

public static class NotificationPublisher
{
    public static Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken,
        IReadOnlyList<INotificationHandler<TNotification>> handlers) where TNotification : INotification
    {
        return handlers.Count switch
        {
            0 => Task.CompletedTask,
            1 => handlers[0].Handle(notification, cancellationToken),
            _ => PublishMany(notification, cancellationToken, handlers)
        };
    }

    private static Task PublishMany<TNotification>(TNotification notification, CancellationToken cancellationToken,
        IReadOnlyList<INotificationHandler<TNotification>> handlers) where TNotification : INotification
    {
        var tasks = new Task[handlers.Count];

        for (var index = 0; index < handlers.Count; index++)
        {
            tasks[index] = handlers[index].Handle(notification, cancellationToken);
        }

        return Task.WhenAll(tasks);
    }
}