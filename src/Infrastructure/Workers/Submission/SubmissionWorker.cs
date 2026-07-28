using Application.Abstractions.Queue;
using Application.Abstractions.Submission;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.Workers.Submission;

public class SubmissionWorker : BackgroundService
{


  private readonly ISubmissionQueue _queue;

  private readonly IServiceProvider _serviceProvider;


  public SubmissionWorker(ISubmissionQueue queue, IServiceProvider serviceProvider)
  {
    _queue = queue;
    _serviceProvider = serviceProvider;
  }

  protected override async Task ExecuteAsync(
   CancellationToken token)
  {
    var tasks = Enumerable.Range(0, 8)
        .Select(_ => ConsumeAsync(token));

    await Task.WhenAll(tasks);
  }

  private async Task ConsumeAsync(
      CancellationToken token)
  {
    await foreach (var operation in _queue.Operations.Reader.ReadAllAsync(token))
    {
      using var scope = _serviceProvider.CreateScope();

      var processor =
          scope.ServiceProvider.GetRequiredService<ISubmissionProcessor>();

      await processor.SubmitOperationAsync(
          operation,
          token);
    }
  }


}
