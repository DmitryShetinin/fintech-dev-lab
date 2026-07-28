
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Application.Abstractions.Submission;




namespace Infrastructure.BackgroundServices;


public class SubmissionBackgroundService : BackgroundService
{
  private readonly IServiceProvider _serviceProvider;

  protected override async Task ExecuteAsync(
      CancellationToken token)
  {
    while (!token.IsCancellationRequested)
    {
      using var scope = _serviceProvider.CreateScope();

      var processor = scope.ServiceProvider
          .GetRequiredService<ISubmissionProcessor>();

      await processor.SubmitOperationAsync(token);

      await Task.Delay(
          TimeSpan.FromSeconds(1),
          token);
    }
  }
}
