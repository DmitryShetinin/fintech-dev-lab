using System.Threading.Channels;
using Application.Abstractions.Queue;
using Core.Models;


namespace Infrastructure.Queue;


public sealed class ReceiptQueue : IReceiptQueue
{
    private readonly Channel<Operation> _channel;


    public ReceiptQueue()
    {
        _channel =
            Channel.CreateUnbounded<Operation>(
                new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = false
                });
    }



    public async ValueTask EnqueueAsync(
        Operation operation,
        CancellationToken cancellationToken)
    {
        await _channel.Writer.WriteAsync(
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