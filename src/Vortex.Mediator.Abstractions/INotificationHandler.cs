namespace Vortex.Mediator.Abstractions;

/// <summary>
/// Handles a notification published through the mediator.
/// </summary>
/// <typeparam name="TNotification">The notification type handled by this implementation.</typeparam>
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    /// <summary>
    /// Handles the specified notification.
    /// </summary>
    /// <param name="notification">The notification to handle.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the notification has been handled.</returns>
    Task Handle(
        TNotification notification,
        CancellationToken cancellationToken);
}
