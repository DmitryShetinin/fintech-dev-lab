using System.Threading.Channels;
using Application.Abstractions.Queue;
using Core.Models;

namespace Infrastructure.Queue;

public sealed class SubmissionQueue : ISubmissionQueue
{
    private readonly Channel<string> _channel;


    public SubmissionQueue()
    {
        _channel =
            Channel.CreateUnbounded<string>(
                new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = false
                });
    }


    public ValueTask EnqueueAsync(
        string operation,
        CancellationToken cancellationToken)
    {
        return _channel.Writer.WriteAsync(
            operation,
            cancellationToken);
    }


    public IAsyncEnumerable<string> ReadAllAsync(
        CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(
            cancellationToken);
    }

        public ValueTask<string> DequeueAsync(
        CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAsync(cancellationToken);
    }
    
}