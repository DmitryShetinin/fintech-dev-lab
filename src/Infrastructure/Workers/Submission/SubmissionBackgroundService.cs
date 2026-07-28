using Application.Abstractions.Queue;
using Application.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;






namespace Infrastructure.BackgroundServices;

public sealed class SubmissionBackgroundService : BackgroundService
{
  private readonly IServiceProvider _serviceProvider;

  public SubmissionBackgroundService(IServiceProvider serviceProvider)
  {
    _serviceProvider = serviceProvider;
  }

  protected override async Task ExecuteAsync(
      CancellationToken token)
  {
    while (!token.IsCancellationRequested)
    {
      using var scope = _serviceProvider.CreateScope();

      var repository =
          scope.ServiceProvider.GetRequiredService<IOperationRepository>();

      var queue =
          scope.ServiceProvider.GetRequiredService<ISubmissionQueue>();

      var operations =
          await repository.GetProcessingAsync(
              DateTime.UtcNow,
              token);

      foreach (var operation in operations)
      {
        await queue.Operations.Writer.WriteAsync(
            operation,
            token);
      }

      await Task.Delay(
          TimeSpan.FromSeconds(1),
          token);
    }
  }
}
