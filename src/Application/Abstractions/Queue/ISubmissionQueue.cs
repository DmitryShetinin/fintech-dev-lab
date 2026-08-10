
using System.Threading.Channels;
using Core.Models;

namespace Application.Abstractions.Queue;



public interface ISubmissionQueue
{
    ValueTask EnqueueAsync(
        string operation,
        CancellationToken cancellationToken);


    IAsyncEnumerable<string> ReadAllAsync(
        CancellationToken cancellationToken);

    ValueTask<string> DequeueAsync(
CancellationToken cancellationToken);

}