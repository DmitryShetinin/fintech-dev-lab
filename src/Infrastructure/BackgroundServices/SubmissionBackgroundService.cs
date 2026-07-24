//namespace Infrastructure.BackgroundServices;

using Application.Abstractions.Providers;
using Application.Extensions;
using Application.Interface;
using Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class SubmissionBackgroundService : BackgroundService
{
  private readonly ILogger<SubmissionBackgroundService> _logger;
  private readonly IServiceProvider _serviceProvider; // чтобы создать scope


  public SubmissionBackgroundService(ILogger<SubmissionBackgroundService> logger, IServiceProvider serviceProvider)
  {
    _logger = logger;
    _serviceProvider = serviceProvider;

  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    _logger.LogInformation("Submission Background Service started.");

    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        using var scope = _serviceProvider.CreateScope();

        var repository =
            scope.ServiceProvider.GetRequiredService<IOperationRepository>();

        var unitOfWork =
            scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var now = DateTime.UtcNow;

        var operations =
            await repository.GetProcessingAsync(
                now,
                stoppingToken);

        foreach (var operation in operations)
        {
          try
          {
            operation.ScheduleNextRetry(
          now,
          TimeSpan.FromSeconds(1));


            await unitOfWork.SaveChangesAsync(stoppingToken);
            // TODO:
            // вызвать ProviderClient
            // обработать ответ
            var result = await _providerClient.CreatePaymentAsync(
                  operation.ToProviderPaymentRequest(),
                  stoppingToken);


            if (!result.IsSuccess)
            {
              continue;
            }

            operation.AttachProviderPayment(result.Value.ProviderPaymentId);


            await unitOfWork.SaveChangesAsync(stoppingToken);

          }
          catch (Exception ex)
          {
            _logger.LogError($"Error {ex.Message}");
          }


        }

        await Task.Delay(
            TimeSpan.FromSeconds(1),
            stoppingToken);
      }
      catch (Exception ex) when (ex is not OperationCanceledException)
      {
        _logger.LogError(ex, "Error in Submission Background Service");

        await Task.Delay(
            TimeSpan.FromSeconds(5),
            stoppingToken);
      }
    }

    _logger.LogInformation("Submission Background Service stopped.");
  }

}
