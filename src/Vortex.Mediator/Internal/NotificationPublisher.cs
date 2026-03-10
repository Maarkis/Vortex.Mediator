using Vortex.Mediator.Abstractions;

namespace Vortex.Mediator.Internal;

/// <summary>
/// Publishes notifications to all resolved handlers.
/// </summary>
public static class NotificationPublisher
{
    /// <summary>
    /// Publishes a notification to the provided handlers.
    /// </summary>
    /// <typeparam name="TNotification">The notification type being published.</typeparam>
    /// <param name="notification">The notification instance to publish.</param>
    /// <param name="cancellationToken">The cancellation token for the current operation.</param>
    /// <param name="handlers">The handlers that should process the notification.</param>
    /// <returns>A task that completes when all handlers have finished processing the notification.</returns>
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
