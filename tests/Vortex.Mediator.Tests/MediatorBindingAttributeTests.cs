using NUnit.Framework;

namespace Vortex.Mediator.Tests;

public sealed class MediatorBindingAttributeTests
{
    [Test]
    public void ConstructorStoresBindingType()
    {
        var attribute = new MediatorBindingAttribute(typeof(SampleBinding));

        Assert.That(attribute.BindingType, Is.EqualTo(typeof(SampleBinding)));
    }

    private sealed class SampleBinding : IMediatorBinding
    {
        public bool TryDispatch<TResponse>(Vortex.Mediator.Abstractions.IRequest<TResponse> request,
            IServiceProvider provider, CancellationToken cancellationToken, out Task<TResponse>? task)
        {
            task = null;
            return false;
        }

        public bool TryDispatch(Vortex.Mediator.Abstractions.IRequest request, IServiceProvider provider,
            CancellationToken cancellationToken, out Task? task)
        {
            task = null;
            return false;
        }

        public bool TryCreateStream<TResponse>(Vortex.Mediator.Abstractions.IStreamRequest<TResponse> request,
            IServiceProvider provider, CancellationToken cancellationToken, out IAsyncEnumerable<TResponse>? stream)
        {
            stream = null;
            return false;
        }

        public bool TryPublish(Vortex.Mediator.Abstractions.INotification notification, IServiceProvider provider,
            CancellationToken cancellationToken, out Task? task)
        {
            task = null;
            return false;
        }
    }
}