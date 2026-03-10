using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Vortex.Mediator.Abstractions;
using Vortex.Mediator.Internal;

namespace Vortex.Mediator.Tests;

public sealed class PipelineExecutorTests
{
    [Test]
    public async Task ExecuteReturnsResponseWhenThereAreNoBehaviors()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var handler = new ResponseHandler();
        var response = await PipelineExecutor.Execute(new ResponseRequest("Ada"), CancellationToken.None, handler, services);

        Assert.That(response, Is.EqualTo("Hello Ada"));
    }

    [Test]
    public async Task ExecuteInvokesResponseBehaviorsInOrder()
    {
        var recorder = new List<string>();
        var services = new ServiceCollection()
            .AddSingleton(recorder)
            .AddSingleton<IPipelineBehavior<ResponseRequest, string>, ResponseOuterBehavior>()
            .AddSingleton<IPipelineBehavior<ResponseRequest, string>, ResponseInnerBehavior>()
            .BuildServiceProvider();

        _ = await PipelineExecutor.Execute(new ResponseRequest("Ada"), CancellationToken.None, new RecordingResponseHandler(recorder), services);

        Assert.That(recorder, Is.EqualTo(new[]
        {
            "response-outer:before",
            "response-inner:before",
            "response-handler:Ada",
            "response-inner:after",
            "response-outer:after"
        }));
    }

    [Test]
    public async Task ExecuteInvokesCommandBehaviorsInOrder()
    {
        var recorder = new List<string>();
        var services = new ServiceCollection()
            .AddSingleton(recorder)
            .AddSingleton<IPipelineBehavior<CommandRequest>, CommandOuterBehavior>()
            .AddSingleton<IPipelineBehavior<CommandRequest>, CommandInnerBehavior>()
            .BuildServiceProvider();

        await PipelineExecutor.Execute(new CommandRequest("Ada"), CancellationToken.None, new RecordingCommandHandler(recorder), services);

        Assert.That(recorder, Is.EqualTo(new[]
        {
            "command-outer:before",
            "command-inner:before",
            "command-handler:Ada",
            "command-inner:after",
            "command-outer:after"
        }));
    }

    [Test]
    public async Task ExecuteStreamInvokesStreamBehaviorsInOrder()
    {
        var recorder = new List<string>();
        var services = new ServiceCollection()
            .AddSingleton(recorder)
            .AddSingleton<IStreamPipelineBehavior<StreamRequest, int>, StreamOuterBehavior>()
            .AddSingleton<IStreamPipelineBehavior<StreamRequest, int>, StreamInnerBehavior>()
            .BuildServiceProvider();

        _ = await ToListAsync(PipelineExecutor.ExecuteStream(new StreamRequest(2), CancellationToken.None, new RecordingStreamHandler(recorder), services));

        Assert.That(recorder, Is.EqualTo(new[]
        {
            "stream-outer:before",
            "stream-inner:before",
            "stream-handler:2",
            "stream-inner:after",
            "stream-outer:after"
        }));
    }

    private static async Task<IReadOnlyList<T>> ToListAsync<T>(IAsyncEnumerable<T> source)
    {
        var items = new List<T>();

        await foreach (var item in source)
        {
            items.Add(item);
        }

        return items;
    }

    public sealed record ResponseRequest(string Name) : IRequest<string>;

    public sealed record CommandRequest(string Name) : IRequest;

    public sealed record StreamRequest(int Count) : IStreamRequest<int>;

    private sealed class ResponseHandler : IRequestHandler<ResponseRequest, string>
    {
        public Task<string> Handle(ResponseRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult($"Hello {request.Name}");
        }
    }

    private sealed class RecordingResponseHandler(List<string> recorder) : IRequestHandler<ResponseRequest, string>
    {
        public Task<string> Handle(ResponseRequest request, CancellationToken cancellationToken)
        {
            recorder.Add($"response-handler:{request.Name}");
            return Task.FromResult($"Hello {request.Name}");
        }
    }

    private sealed class RecordingCommandHandler(List<string> recorder) : IRequestHandler<CommandRequest>
    {
        public Task Handle(CommandRequest request, CancellationToken cancellationToken)
        {
            recorder.Add($"command-handler:{request.Name}");
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingStreamHandler(List<string> recorder) : IStreamRequestHandler<StreamRequest, int>
    {
        public IAsyncEnumerable<int> Handle(StreamRequest request, CancellationToken cancellationToken)
        {
            recorder.Add($"stream-handler:{request.Count}");
            return Execute(request.Count, cancellationToken);
        }

        private static async IAsyncEnumerable<int> Execute(int count, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (var index = 1; index <= count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return index;
                await Task.Yield();
            }
        }
    }

    private sealed class ResponseOuterBehavior(List<string> recorder) : IPipelineBehavior<ResponseRequest, string>
    {
        public async Task<string> Handle(ResponseRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        {
            recorder.Add("response-outer:before");
            var response = await next();
            recorder.Add("response-outer:after");
            return response;
        }
    }

    private sealed class ResponseInnerBehavior(List<string> recorder) : IPipelineBehavior<ResponseRequest, string>
    {
        public async Task<string> Handle(ResponseRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        {
            recorder.Add("response-inner:before");
            var response = await next();
            recorder.Add("response-inner:after");
            return response;
        }
    }

    private sealed class CommandOuterBehavior(List<string> recorder) : IPipelineBehavior<CommandRequest>
    {
        public async Task Handle(CommandRequest request, RequestHandlerDelegate next, CancellationToken cancellationToken)
        {
            recorder.Add("command-outer:before");
            await next();
            recorder.Add("command-outer:after");
        }
    }

    private sealed class CommandInnerBehavior(List<string> recorder) : IPipelineBehavior<CommandRequest>
    {
        public async Task Handle(CommandRequest request, RequestHandlerDelegate next, CancellationToken cancellationToken)
        {
            recorder.Add("command-inner:before");
            await next();
            recorder.Add("command-inner:after");
        }
    }

    private sealed class StreamOuterBehavior(List<string> recorder) : IStreamPipelineBehavior<StreamRequest, int>
    {
        public IAsyncEnumerable<int> Handle(StreamRequest request, StreamHandlerDelegate<int> next, CancellationToken cancellationToken)
        {
            recorder.Add("stream-outer:before");
            var stream = next();
            recorder.Add("stream-outer:after");
            return stream;
        }
    }

    private sealed class StreamInnerBehavior(List<string> recorder) : IStreamPipelineBehavior<StreamRequest, int>
    {
        public IAsyncEnumerable<int> Handle(StreamRequest request, StreamHandlerDelegate<int> next, CancellationToken cancellationToken)
        {
            recorder.Add("stream-inner:before");
            var stream = next();
            recorder.Add("stream-inner:after");
            return stream;
        }
    }
}
