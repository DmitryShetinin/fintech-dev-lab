using Core.Models;

namespace Application.Abstractions.Queue;

public interface IReceiptQueue
{
    ValueTask EnqueueAsync(
        Operation operation,
        CancellationToken cancellationToken);


    IAsyncEnumerable<Operation> ReadAllAsync(
        CancellationToken cancellationToken);
}