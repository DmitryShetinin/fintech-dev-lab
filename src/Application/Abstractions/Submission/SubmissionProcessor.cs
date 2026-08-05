using Application.Abstractions.Persistence;
using Application.Abstractions.Providers;
using Application.Abstractions.Retry;
using Application.Common.Failures;
using Application.Extensions;
using Application.Interface;
using Application.Provider;
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



    public SubmissionProcessor(
        IPaymentAttemptRepository attemptRepository,
        IProviderClientFactory providerFactory,
        IRetryPolicy retryPolicy,
        IUnitOfWork unitOfWork,
        OperationStateMachine stateMachine,
        ILogger<SubmissionProcessor> logger)
    {
        _paymentAttemptRepository = attemptRepository;
        _providerFactory = providerFactory;
        _retryPolicy = retryPolicy;
        _unitOfWork = unitOfWork;
        _stateMachine = stateMachine;
        _logger = logger;
    }




    public async Task SubmitOperationAsync(
        Operation operation,
        CancellationToken stoppingToken)
    {
        var provider =
            _providerFactory.Get(operation.Provider);



        var attemptNumber =
            await _paymentAttemptRepository.GetNextAttemptNumberAsync(
                operation.Id,
                PaymentAttemptType.Submission,
                stoppingToken);



        var attempt = PaymentAttempt.Start(
            operation.Id,
            attemptNumber,
            PaymentAttemptType.Submission);



        await _paymentAttemptRepository.AddAsync(
            attempt,
            stoppingToken);


        await _unitOfWork.SaveChangesAsync(
            stoppingToken);



        var result =
            await provider.CreatePaymentAsync(
                operation.ToProviderRequest(),
                stoppingToken);



        if (!result.IsSuccess)
        {
            var failure =
                result.GetError<ProviderFailure>();


            await HandleFailure(
                attempt,
                failure!,
                stoppingToken);


            return;
        }



        var response =
            result.Value!;



        HandleProviderAccepted(
            operation,
            attempt,
            response);



        await _unitOfWork.SaveChangesAsync(
            stoppingToken);
    }





    private async Task HandleFailure(
        PaymentAttempt attempt,
        ProviderFailure failure,
        CancellationToken ct)
    {
        attempt.Fail(
            failure.Reason,
            failure.Message);



        if (!_retryPolicy.CanRetry(
            failure.Reason,
            attempt.AttemptNumber))
        {
            await _unitOfWork.SaveChangesAsync(ct);


            _logger.LogError(
                "Submission failed permanently. Operation {OperationId}. Reason {Reason}",
                attempt.OperationId,
                failure.Reason);


            return;
        }



        var delay =
            _retryPolicy.GetRetryDelay(
                attempt.AttemptNumber);



        attempt.ScheduleRetry(
                DateTime.UtcNow,
                delay);



        await _unitOfWork.SaveChangesAsync(ct);



        _logger.LogWarning(
            "Submission failed. Operation {OperationId}. Reason {Reason}. Retry after {Delay}",
            attempt.OperationId,
            failure.Reason,
            delay);
    }





    private void HandleProviderAccepted(
        Operation operation,
        PaymentAttempt attempt,
        ProviderResponse response)
    {
        operation.WaitForReceipt(
            _stateMachine,
            response.ProviderPaymentId!);



        attempt.MarkProviderAccepted(
            response.ProviderPaymentId!);
    }
}