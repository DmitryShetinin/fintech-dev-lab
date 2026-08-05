using Application.Abstractions.Providers;
using Application.Abstractions.Persistence;
using Application.Abstractions.Retry;
using Application.Common.Failures;
using Application.Interface;
using Core.Models;
using Microsoft.Extensions.Logging;
using Core.Enums;


namespace Application.Abstractions.Receipt;


public class ReceiptProcessor : IReceiptProcessor
{
    private readonly IPaymentAttemptRepository _paymentAttemptRepository;
    private readonly IProviderClientFactory _providerFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly OperationStateMachine _stateMachine;
    private readonly IRetryPolicy _retryPolicy;
    private readonly ILogger<ReceiptProcessor> _logger;


    public ReceiptProcessor(
        IPaymentAttemptRepository paymentAttemptRepository,
        IProviderClientFactory providerFactory,
        IUnitOfWork unitOfWork,
        OperationStateMachine stateMachine,
        IRetryPolicy retryPolicy,
        ILogger<ReceiptProcessor> logger)
    {
        _paymentAttemptRepository = paymentAttemptRepository;
        _providerFactory = providerFactory;
        _unitOfWork = unitOfWork;
        _stateMachine = stateMachine;
        _retryPolicy = retryPolicy;
        _logger = logger;
    }



    public async Task ProcessAsync(
        Operation operation,
        CancellationToken token)
    {
        var provider =
            _providerFactory.Get(
                operation.Provider);



        var attemptNumber =
            await _paymentAttemptRepository
                .GetNextAttemptNumberAsync(
                    operation.Id,
                    PaymentAttemptType.ReceiptPolling,
                    token);



        var attempt =
            PaymentAttempt.Start(
                operation.Id,
                attemptNumber,
                PaymentAttemptType.ReceiptPolling);



        await _paymentAttemptRepository.AddAsync(
            attempt,
            token);



        await _unitOfWork.SaveChangesAsync(
            token);



        var result =
            await provider.GetPaymentStatusAsync(
                operation.ProviderPaymentId!,
                token);



        if (!result.IsSuccess)
        {
            var failure =
                result.GetError<ProviderFailure>();


            await HandleFailure(
                attempt,
                failure!,
                token);


            return;
        }



        var response =
            result.Value!;



        switch (response.Status)
        {
            case ProviderPaymentStatus.Succeeded:

                operation.Complete(
                    _stateMachine);


                attempt.Complete();

                break;



            case ProviderPaymentStatus.Failed:

                operation.Reject(
                    _stateMachine);


                attempt.Complete();

                break;



            case ProviderPaymentStatus.Pending:

                ScheduleRetry(
                    attempt);

                break;
        }



        await _unitOfWork.SaveChangesAsync(
            token);
    }





    private async Task HandleFailure(
        PaymentAttempt attempt,
        ProviderFailure failure,
        CancellationToken token)
    {
        attempt.Fail(
            failure.Reason,
            failure.Message);



        if (!_retryPolicy.CanRetry(
                failure.Reason,
                attempt.AttemptNumber))
        {
            await _unitOfWork.SaveChangesAsync(
                token);


            _logger.LogError(
                "Receipt polling permanently failed. Operation {OperationId}. Reason {Reason}",
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



        await _unitOfWork.SaveChangesAsync(
            token);



        _logger.LogWarning(
            "Receipt polling failed for operation {OperationId}. Retry after {Delay}",
            attempt.OperationId,
            delay);
    }




    private void ScheduleRetry(
        PaymentAttempt attempt)
    {
        var delay =
            _retryPolicy.GetRetryDelay(
                attempt.AttemptNumber);


        attempt.ScheduleRetry(
     DateTime.UtcNow,
     delay);
    }
}