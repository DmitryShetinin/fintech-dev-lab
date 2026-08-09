using Application.Abstractions.Persistence;
using Application.Abstractions.Providers;
using Application.Abstractions.Retry;
using Application.Abstractions.Telemetry;
using Application.Common;
using Application.Common.Failures;
using Application.Extensions;
using Application.Interface;
using Application.Provider;
using Core.Enums;
using Core.Models;
using Microsoft.Extensions.Logging;


namespace Application.Abstractions.Submission;


public class SubmissionProcessor : ISubmissionProcessor
{
    private readonly IPaymentAttemptRepository _paymentAttemptRepository;
    private readonly IProviderClientFactory _providerFactory;
    private readonly IRetryPolicy _retryPolicy;
    private readonly IUnitOfWork _unitOfWork;
    private readonly OperationStateMachine _stateMachine;
    private readonly ILogger<SubmissionProcessor> _logger;
    private readonly IOperationMetrics _operationMetrics;

    public SubmissionProcessor(
        IPaymentAttemptRepository attemptRepository,
        IProviderClientFactory providerFactory,
        IRetryPolicy retryPolicy,
        IUnitOfWork unitOfWork,
        OperationStateMachine stateMachine,
        ILogger<SubmissionProcessor> logger, 
        IOperationMetrics operationMetrics)
    {
        _paymentAttemptRepository = attemptRepository;
        _providerFactory = providerFactory;
        _retryPolicy = retryPolicy;
        _unitOfWork = unitOfWork;
        _stateMachine = stateMachine;
        _logger = logger;
        _operationMetrics = operationMetrics;
    }




    public async Task SubmitOperationAsync(
        Operation operation,
        CancellationToken token)
    {
        var provider =
            _providerFactory.Get(operation.Provider);




_logger.LogInformation(
    "Sending payment to provider. OperationId={OperationId}, Provider={Provider}",
    operation.Id,
    operation.Provider);

        var attempt = await CreateAttemptAsync(operation, token);


        await _unitOfWork.ExecuteInTransactionAsync(
        async ct =>
        {
            await _paymentAttemptRepository.AddAsync(
                attempt,
                ct);
        },
        token);


        Result<ProviderResponse> result;


        try
        {
            result = await provider.CreatePaymentAsync(
                operation.ToProviderRequest(),
                token);
        }
        catch (TaskCanceledException) when (!token.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Provider request timed out. OperationId={OperationId}",
                operation.Id);

            var failure = new ProviderFailure(
                ProviderFailureReason.Timeout,
                "Provider request timed out.");

            await _unitOfWork.ExecuteInTransactionAsync(
                async ct =>
                {
                    HandleFailure(
                        attempt,
                        failure,
                        operation);

                    await Task.CompletedTask;
                },
                token);

            return;
        }



        await _unitOfWork.ExecuteInTransactionAsync(
        async ct =>
        {
            if (!result.IsSuccess)
            {
                var failure =
              result.GetError<ProviderFailure>();

                if (failure is null)
                    throw new InvalidOperationException("Provider returned failure but error object is missing.");

                HandleFailure(
                    attempt,
                    failure,
                    operation);
            }
            else
            {
                var response = result.Value!.ProviderPaymentId!;
                operation.SetProviderPaymentId(response);
                attempt.MarkProviderAccepted(
                    response);
            }
        },
        token);




    }



    private async Task<PaymentAttempt> CreateAttemptAsync(Operation operation, CancellationToken token)
    {

        var attemptNumber =
            await _paymentAttemptRepository.GetNextAttemptNumberAsync(
                operation.Id,
                PaymentAttemptType.ReceiptPolling,
                token);

        var attempt =
            PaymentAttempt.Start(
                operation.Id,
                attemptNumber,
                PaymentAttemptType.ReceiptPolling);

        return attempt;
    }

    private void HandleFailure(
        PaymentAttempt attempt,
        ProviderFailure failure,
        Operation operation)
    {
        attempt.Fail(
            failure.Reason,
            failure.Message);



        if (!_retryPolicy.CanRetry(
            failure.Reason,
            attempt.AttemptNumber))
        {


            _logger.LogError(
                "Submission failed permanently. Operation {OperationId}. Reason {Reason}",
                attempt.OperationId,
                failure.Reason);


            return;
        }



        var delay =
            _retryPolicy.GetRetryDelay(
                attempt.AttemptNumber);



        operation.ScheduleRetry(delay);
        _operationMetrics.AddRetryOccurred();






        _logger.LogWarning(
            "Submission failed. Operation {OperationId}. Reason {Reason}. Retry after {Delay}",
            attempt.OperationId,
            failure.Reason,
            delay);
    }






}