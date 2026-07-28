using System.Threading.Channels;
using Core.Models;

namespace Infrastructure.Workers.Submission.Queue;

public interface ISubmissionQueue
{
  Channel<Operation> Operations { get; }
}
