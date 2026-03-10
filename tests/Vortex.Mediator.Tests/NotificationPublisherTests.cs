using NUnit.Framework;
using Vortex.Mediator.Abstractions;
using Vortex.Mediator.Internal;

namespace Vortex.Mediator.Tests;

public sealed class NotificationPublisherTests
{
    [Test]
    public async Task PublishReturnsCompletedTaskWhenThereAreNoHandlers()
    {
        var task = NotificationPublisher.Publish(new TestNotification(), CancellationToken.None, Array.Empty<INotificationHandler<TestNotification>>());

        await task;

        Assert.That(task.IsCompletedSuccessfully, Is.True);
    }

    [Test]
    public async Task PublishInvokesSingleHandler()
    {
        var recorder = new List<string>();
        var handlers = new INotificationHandler<TestNotification>[]
        {
            new RecordingNotificationHandler("one", recorder)
        };

        await NotificationPublisher.Publish(new TestNotification(), CancellationToken.None, handlers);

        Assert.That(recorder, Is.EqualTo(new[] { "one" }));
    }

    [Test]
    public async Task PublishInvokesAllHandlers()
    {
        var recorder = new List<string>();
        var handlers = new INotificationHandler<TestNotification>[]
        {
            new RecordingNotificationHandler("one", recorder),
            new RecordingNotificationHandler("two", recorder)
        };

        await NotificationPublisher.Publish(new TestNotification(), CancellationToken.None, handlers);

        Assert.That(recorder, Is.EqualTo(new[] { "one", "two" }));
    }

    [Test]
    public void PublishPropagatesExceptionFromHandlers()
    {
        var handlers = new INotificationHandler<TestNotification>[]
        {
            new ThrowingNotificationHandler()
        };
        var act = async () => await NotificationPublisher.Publish(new TestNotification(), CancellationToken.None, handlers);

        Assert.That(act, Throws.TypeOf<InvalidOperationException>());
    }

    public sealed record TestNotification : INotification;

    private sealed class RecordingNotificationHandler(string name, List<string> recorder) : INotificationHandler<TestNotification>
    {
        public Task Handle(TestNotification notification, CancellationToken cancellationToken)
        {
            recorder.Add(name);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingNotificationHandler : INotificationHandler<TestNotification>
    {
        public Task Handle(TestNotification notification, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("notification failure");
        }
    }
}
