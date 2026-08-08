
using System.Threading.Channels;
using Core.Models;

namespace Application.Abstractions.Queue;



public interface ISubmissionQueue
{
    ValueTask EnqueueAsync(
        Operation operation,
        CancellationToken cancellationToken);


    IAsyncEnumerable<Operation> ReadAllAsync(
        CancellationToken cancellationToken);

    ValueTask<Operation> DequeueAsync(
CancellationToken cancellationToken);

}