using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Vortex.Mediator.Abstractions;

namespace Vortex.Mediator.Tests;

public sealed class ReentrancyTests
{
    [Test]
    public async Task SendSupportsNestedMediatorDispatch()
    {
        var services = new ServiceCollection();
        services.AddVortexMediator();
        services.AddScoped<IRequestHandler<OuterQuery, string>, OuterQueryHandler>();
        services.AddScoped<IRequestHandler<InnerQuery, string>, InnerQueryHandler>();

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new OuterQuery("Ada"));

        Assert.That(response, Is.EqualTo("outer:inner:Ada"));
    }

    public sealed record OuterQuery(string Name) : IRequest<string>;

    public sealed record InnerQuery(string Name) : IRequest<string>;

    private sealed class OuterQueryHandler(IMediator mediator) : IRequestHandler<OuterQuery, string>
    {
        public async Task<string> Handle(OuterQuery request, CancellationToken cancellationToken)
        {
            var inner = await mediator.Send(new InnerQuery(request.Name), cancellationToken);
            return $"outer:{inner}";
        }
    }

    private sealed class InnerQueryHandler : IRequestHandler<InnerQuery, string>
    {
        public Task<string> Handle(InnerQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult($"inner:{request.Name}");
        }
    }
}
