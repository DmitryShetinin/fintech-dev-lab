using Application.Abstractions.Persistence;
using Application.Abstractions.Providers;
using Application.Abstractions.Retry;
using Application.Extensions;
using Application.Interface;
using Application.Provider;
using Core.Enums;
using Core.Models;
using Microsoft.Extensions.Logging;



namespace Application.Abstractions.Submission;


public class SubmissionProcessor : ISubmissionProcessor
{
  private readonly IOperationRepository _operationRepository;
  private readonly IPaymentAttemptRepository _paymentAttemptRepository;
  private readonly IProviderClientFactory _providerFactory;
  private readonly IRetryPolicy _retryPolicy;
  private readonly IUnitOfWork _unitOfWork;
  private readonly OperationStateMachine _stateMachine;
  private readonly ILogger<SubmissionProcessor> _logger;


  public SubmissionProcessor(
      IOperationRepository operationRepository,
      IPaymentAttemptRepository attemptRepository,
      IProviderClientFactory providerFactory,
      IRetryPolicy retryPolicy,
      IUnitOfWork unitOfWork, OperationStateMachine stateMachine)
  {
    _operationRepository = operationRepository;
    _paymentAttemptRepository = attemptRepository;
    _providerFactory = providerFactory;
    _retryPolicy = retryPolicy;
    _unitOfWork = unitOfWork;
    _stateMachine = stateMachine;
  }


  public async Task SubmitOperationAsync(
      CancellationToken token)
  {
    var operations =
        await _operationRepository
            .GetProcessingAsync(
                DateTime.UtcNow,
                token);


    foreach (var operation in operations)
    {
      await ProcessOperationAsync(
          operation,
          token);
    }
  }


  private async Task ProcessOperationAsync(
     Operation operation,

     CancellationToken stoppingToken)
  {
    var provider = _providerFactory.Get(operation.Provider);

    var attempt = PaymentAttempt.Start(
        operation.Id,
        operation.RetryCount + 1);

    await _paymentAttemptRepository.AddAsync(
        attempt,
        stoppingToken);

    await _unitOfWork.SaveChangesAsync(
        stoppingToken);



    var result = await provider.CreatePaymentAsync(
        operation.ToProviderRequest(),
        stoppingToken);


    if (!result.IsSuccess)
    {
      await HandleProviderCommunicationFailure(
          operation,
          attempt,
          result.Error!,
                stoppingToken);

      return;
    }


    var response = result.Value!;


    if (provider.IsTransientFailure(response))
    {
      await HandleTransientProviderFailure(
          operation,
          attempt,
          response,
                 stoppingToken);

      return;
    }


    HandleProviderAccepted(
        operation,
        attempt,
        response);


    await _unitOfWork.SaveChangesAsync(
        stoppingToken);
  }




  private async Task HandleProviderCommunicationFailure(
      Operation operation,
      PaymentAttempt attempt,
      string error,
      CancellationToken stoppingToken)
  {
    attempt.Fail(
        ProviderFailureReason.Network,
        error);



    var delay = _retryPolicy.GetRetryDelay(
      operation.RetryCount + 1);

    operation.ScheduleNextRetry(
      DateTime.UtcNow,
      delay);


    await _unitOfWork.SaveChangesAsync(
        stoppingToken);


    _logger.LogWarning(
        "Failed to submit operation {OperationId}. Error: {Error}. Retry after {Delay}",
        operation.Id,
        error,
        delay);
  }

  private async Task HandleTransientProviderFailure(
      Operation operation,
      PaymentAttempt attempt,
      ProviderResponse response,
      CancellationToken stoppingToken)
  {
    attempt.Fail(
        ProviderFailureReason.Http,
        $"Provider returned {(int?)response.HttpStatusCode}");


    var delay = _retryPolicy.GetRetryDelay(
        operation.RetryCount + 1);


    operation.ScheduleNextRetry(
        DateTime.UtcNow,
        delay);


    await _unitOfWork.SaveChangesAsync(
        stoppingToken);


    _logger.LogWarning(
        "Provider temporary failure for operation {OperationId}. Retry after {Delay}",
        operation.Id,
        delay);
  }

  private void HandleProviderAccepted(
      Operation operation,
      PaymentAttempt attempt,
      ProviderResponse response)
  {
    operation.WaitForReceipt(
        _stateMachine,
        response.ProviderPaymentId);


    attempt.MarkProviderAccepted(
        response.ProviderPaymentId);
  }




}
