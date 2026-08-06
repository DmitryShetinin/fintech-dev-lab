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
            _providerFactory.Get(operation.Provider);

        var attempt = await CreateAttemptAsync(operation, token);


        // =========================
        // 1. Сохраняем попытку
        // =========================

        await _unitOfWork.ExecuteInTransactionAsync(
        async ct =>
        {
            await _paymentAttemptRepository.AddAsync(
                attempt,
                ct);
        },
        token);

        // =========================
        // 2. Внешний HTTP вызов
        // =========================

        var result =
            await provider.GetPaymentStatusAsync(
                operation.ProviderPaymentId!,
                token);

        // =========================
        // 3. Обрабатываем результат
        // =========================

        await _unitOfWork.ExecuteInTransactionAsync(
        async ct =>
        {
            if (!result.IsSuccess)
            {
                var failure =
                    result.GetError<ProviderFailure>()!;

                HandleFailure(
                    attempt, 
                    failure, 
                    operation);
            }
            else
            {
                HandleProviderResponse(
                    operation,
                    attempt,
                    result.Value!);
            }
        },
        token);
    }




    private void HandleFailure(
        PaymentAttempt attempt,
        ProviderFailure failure, Operation operation)
    {
        
        attempt.Fail(failure.Reason,failure.Message);


        if (!_retryPolicy.CanRetry(
                failure.Reason,
                attempt.AttemptNumber))
        {

  
         
            _stateMachine.Reject(operation); 
            _logger.LogError(
                "Receipt polling permanently failed. Operation {OperationId}. Reason {Reason}",
                attempt.OperationId,
                failure.Reason);


            return;
        }



        var delay =
            _retryPolicy.GetRetryDelay(
                attempt.AttemptNumber);


        
        operation.ScheduleRetry(delay);


 



        _logger.LogWarning(
            "Receipt polling failed for operation {OperationId}. Retry after {Delay}",
            attempt.OperationId,
            delay);
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

    private void HandleProviderResponse(Operation operation,
                                              PaymentAttempt attempt,
                                              ProviderPaymentStatusResponse response)
    {

        switch (response.Status)
        {
            case ProviderPaymentStatus.Succeeded:

                
            
                _stateMachine.Complete(operation);
                attempt.Complete();

                break;

            case ProviderPaymentStatus.Failed:

            
                _stateMachine.Reject(operation);
                attempt.Complete();

                break;

            case ProviderPaymentStatus.Pending:

                var delay =
                _retryPolicy.GetRetryDelay(
                    attempt.AttemptNumber);

                operation.ScheduleRetry(delay);
 
                break;
        }



    }


}