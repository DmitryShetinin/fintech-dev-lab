 
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

public sealed class SubmissionProcessor : ISubmissionProcessor
{
    private readonly IOperationRepository _operationRepository;
    private readonly IPaymentAttemptRepository _paymentAttemptRepository;
    private readonly IProviderClientFactory _providerFactory;
    private readonly IRetryPolicy _retryPolicy;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SubmissionProcessor> _logger;
    private readonly IOperationMetrics _operationMetrics;
    private readonly OperationStateMachine _stateMachine;

    public SubmissionProcessor(
        IOperationRepository operationRepository,
        IPaymentAttemptRepository paymentAttemptRepository,
        IProviderClientFactory providerFactory,
        IRetryPolicy retryPolicy,
        IUnitOfWork unitOfWork,
        ILogger<SubmissionProcessor> logger,
        IOperationMetrics operationMetrics,
        OperationStateMachine stateMachine)
    {
        _operationRepository = operationRepository;
        _paymentAttemptRepository = paymentAttemptRepository;
        _providerFactory = providerFactory;
        _retryPolicy = retryPolicy;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _operationMetrics = operationMetrics;
        _stateMachine = stateMachine;
    }

    public async Task SubmitOperationAsync(
        string operationId,
        CancellationToken token)
    {
        var operation =
            await _operationRepository.GetByIdAsync(
                operationId,
                token);

        if (operation is null)
        {
            _logger.LogError(
                "Operation {OperationId} not found",
                operationId);

            return;
        }

        var provider =
            _providerFactory.Get(operation.Provider);

        var attempt =
            await CreateAttemptAsync(
                operation,
                token);

        await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                await _paymentAttemptRepository.AddAsync(
                    attempt,
                    ct);
            },
            token);

        ProviderFailure? failure = null;
        ProviderResponse? response = null;

        using var timeoutCts =
            CancellationTokenSource.CreateLinkedTokenSource(token);

        timeoutCts.CancelAfter(
            TimeSpan.FromSeconds(10));

        try
        {
            _logger.LogInformation(
                "Sending payment to provider. OperationId={OperationId}, Provider={Provider}",
                operation.Id,
                operation.Provider);

            var result =
                await provider.CreatePaymentAsync(
                    operation.ToProviderRequest(),
                    timeoutCts.Token);

            if (!result.IsSuccess)
            {
                failure =
                    result.GetError<ProviderFailure>();

                if (failure is null)
                {
                    throw new InvalidOperationException(
                        "Provider returned failure but error object is missing.");
                }
            }
            else
            {
                response = result.Value;
            }
        }
        catch (TaskCanceledException)
            when (!token.IsCancellationRequested)
        {
            failure =
                new ProviderFailure(
                    ProviderFailureReason.Timeout,
                    "Provider request timed out.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(
                ex,
                "Provider network request failed. OperationId={OperationId}",
                operation.Id);

            failure =
                new ProviderFailure(
                    ProviderFailureReason.Network,
                    "Provider network request failed.");
        }

        await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                if (failure is not null)
                {
                    HandleFailure(
                        attempt,
                        failure,
                        operation);

                    return;
                }

                if (response is null)
                {
                    throw new InvalidOperationException(
                        "Provider returned neither success response nor failure.");
                }

                operation.SetProviderPaymentId(
                    response.ProviderPaymentId!);

                attempt.MarkProviderAccepted(
                    response.ProviderPaymentId!);
            },
            token);
    }

    private async Task<PaymentAttempt> CreateAttemptAsync(
        Operation operation,
        CancellationToken token)
    {
        var attemptNumber =
            await _paymentAttemptRepository
                .GetNextAttemptNumberAsync(
                    operation.Id,
                    PaymentAttemptType.ReceiptPolling,
                    token);

        return PaymentAttempt.Start(
            operation.Id,
            attemptNumber,
            PaymentAttemptType.ReceiptPolling);
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
                operation.RetryCount))
        {
            _stateMachine.Reject(operation);

            _logger.LogError(
                "Submission failed permanently. Operation {OperationId}. Reason {Reason}",
                operation.Id,
                failure.Reason);

            return;
        }

        var delay =
            _retryPolicy.GetRetryDelay(
                operation.RetryCount);

        operation.ScheduleRetry(
            delay);

        _operationMetrics.AddRetryOccurred();

        _logger.LogWarning(
            "Submission failed. Operation {OperationId}. Reason {Reason}. Retry after {Delay}",
            operation.Id,
            failure.Reason,
            delay);
    }
}
 
