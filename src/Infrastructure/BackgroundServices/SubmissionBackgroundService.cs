
using Microsoft.Extensions.DependencyInjection;
using Application.Abstractions.Providers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Application.Interfaces;
using Application.Extensions;
using Application.Interface;
using Core.Models;
using Application.Abstractions.Retry;




namespace Infrastructure.BackgroundServices;


public class SubmissionBackgroundService : BackgroundService
{
  private readonly ILogger<SubmissionBackgroundService> _logger;
  private readonly IServiceProvider _serviceProvider; // чтобы создать scope
  private readonly IProviderClientFactory _providerFactory;
  private readonly IRetryPolicy _retryPolicy;



  public SubmissionBackgroundService(ILogger<SubmissionBackgroundService> logger, IServiceProvider serviceProvider, IProviderClientFactory providerClientFactory, IRetryPolicy retryPolicy)
  {
    _logger = logger;
    _serviceProvider = serviceProvider;
    _providerFactory = providerClientFactory;
    _retryPolicy = retryPolicy;

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

    var result = await provider.CreatePaymentAsync(
        operation.ToProviderRequest(),
        stoppingToken);


    // Не смогли достучаться до провайдера:
    // timeout, network, dns и т.д.
    if (!result.IsSuccess)
    {
      var delay = _retryPolicy.GetRetryDelay(
          operation.RetryCount);


      operation.ScheduleNextRetry(
          DateTime.UtcNow,
          delay);


      await unitOfWork.SaveChangesAsync(stoppingToken);


      _logger.LogWarning(
          "Failed to submit operation {OperationId}. Error: {Error}. Retry after {Delay}",
          operation.OperationId,
          result.Error,
          delay);


      return;
    }


    var response = result.Value!;


    // Провайдер ответил, но ошибка временная:
    // 429, 500, 503 и т.д.
    if (provider.IsTransientFailure(response))
    {
      var delay = _retryPolicy.GetRetryDelay(
          operation.RetryCount);


      operation.ScheduleNextRetry(
          DateTime.UtcNow,
          delay);


      await unitOfWork.SaveChangesAsync(stoppingToken);


      _logger.LogWarning(
          "Provider temporary failure for operation {OperationId}. Retry after {Delay}",
          operation.OperationId,
          delay);


      return;
    }


    // Успешная отправка
    operation.MarkAsAcceptedByProvider(
        response.ProviderPaymentId);


    await unitOfWork.SaveChangesAsync(stoppingToken);


    _logger.LogInformation(
        "Operation {OperationId} accepted by provider. ProviderPaymentId={ProviderPaymentId}",
        operation.OperationId,
        response.ProviderPaymentId);


  }
}


