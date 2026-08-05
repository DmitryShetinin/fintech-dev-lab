using System.Threading.Channels;
using Application.Abstractions.Queue;
using Core.Models;

namespace Infrastructure.Queue;

public sealed class SubmissionQueue : ISubmissionQueue
{
    private readonly Channel<Operation> _channel;


    public SubmissionQueue()
    {
        _channel =
            Channel.CreateUnbounded<Operation>(
                new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = false
                });
    }


    public ValueTask EnqueueAsync(
        Operation operation,
        CancellationToken cancellationToken)
    {
        return _channel.Writer.WriteAsync(
            operation,
            cancellationToken);
    }


    public IAsyncEnumerable<Operation> ReadAllAsync(
        CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(
            cancellationToken);
    }
}