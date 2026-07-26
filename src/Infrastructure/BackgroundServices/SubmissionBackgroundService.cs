
using Microsoft.Extensions.DependencyInjection;
using Application.Abstractions.Providers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Application.Interfaces;
using Application.Extensions;
using Application.Interface;
using Core.Models;




namespace Infrastructure.BackgroundServices;


public class SubmissionBackgroundService : BackgroundService
{
  private readonly ILogger<SubmissionBackgroundService> _logger;
  private readonly IServiceProvider _serviceProvider; // чтобы создать scope
  private readonly IProviderClientFactory _providerFactory;

  public SubmissionBackgroundService(ILogger<SubmissionBackgroundService> logger, IServiceProvider serviceProvider, IProviderClientFactory providerClientFactory)
  {
    _logger = logger;
    _serviceProvider = serviceProvider;
    _providerFactory = providerClientFactory;

  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    _logger.LogInformation("Submission Background Service started.");

    while (!stoppingToken.IsCancellationRequested)
    {
      using var scope = _serviceProvider.CreateScope();

      var repository = scope.ServiceProvider.GetRequiredService<IOperationRepository>();

      var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();


      var now = DateTime.UtcNow;
      var operations = await repository.GetProcessingAsync(now, stoppingToken);

      foreach (var operation in operations)
      {
        try
        {

          await ProcessOperationAsync(operation, unitOfWork, stoppingToken);
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Error while processing operation {OperationId}", operation.OperationId);
        }


      }

      await Task.Delay(
          TimeSpan.FromSeconds(1),
          stoppingToken);
    }

    _logger.LogInformation("Submission Background Service stopped.");
  }


  private async Task ProcessOperationAsync(
     Operation operation,
     IUnitOfWork unitOfWork,
     CancellationToken stoppingToken)
  {
    var provider = _providerFactory.Get(operation.Provider);

    var response = await provider.CreatePaymentAsync(
        operation.ToProviderPaymentRequest(),
        stoppingToken);

    var decision = provider.GetRetryDecision(
        response.Value,
        operation.RetryCount);

    if (decision.ShouldRetry)
    {
      operation.ScheduleNextRetry(
          DateTime.UtcNow,
          decision.Delay);

      await unitOfWork.SaveChangesAsync(stoppingToken);

      _logger.LogWarning(
          "Retry scheduled for operation {OperationId}. Next attempt in {Delay}.",
          operation.OperationId,
          decision.Delay);

      return;
    }

    if (!response.IsSuccess)
    {
      _logger.LogWarning(
          "Provider rejected operation {OperationId}. Retry is not required.",
          operation.OperationId);

      return;
    }

    var payment = response.Value
        ?? throw new InvalidOperationException(
            "Successful provider response must contain payment information.");

    operation.MarkAsAcceptedByProvider(
        payment.ProviderPaymentId);

    await unitOfWork.SaveChangesAsync(stoppingToken);

    _logger.LogInformation(
        "Operation {OperationId} accepted by provider. ProviderPaymentId={ProviderPaymentId}",
        operation.OperationId,
        payment.ProviderPaymentId);
  }

}


