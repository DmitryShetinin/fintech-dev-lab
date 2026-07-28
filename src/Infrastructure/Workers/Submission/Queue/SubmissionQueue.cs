using System.Threading.Channels;
using Application.Abstractions.Queue;
using Core.Models;

namespace Infrastructure.Workers.Queue;



using System.Threading.Channels;

public sealed class SubmissionQueue : ISubmissionQueue
{
  public Channel<Operation> Operations { get; } =
      Channel.CreateUnbounded<Operation>();
}
