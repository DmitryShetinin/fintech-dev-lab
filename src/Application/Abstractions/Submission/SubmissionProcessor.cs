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
        CancellationToken token)
    {
        var provider =
            _providerFactory.Get(operation.Provider);



       


        var attempt = await CreateAttemptAsync(operation, token); 
    

        await _unitOfWork.ExecuteInTransactionAsync(
        async ct =>
        {
            await _paymentAttemptRepository.AddAsync(
                attempt,
                ct);
        },
        token);

 
 


        var result =
            await provider.CreatePaymentAsync(
                operation.ToProviderRequest(),
                token);



        await _unitOfWork.ExecuteInTransactionAsync(
        async ct =>
        {
            if (!result.IsSuccess)
            {
                  var failure =
                result.GetError<ProviderFailure>();

                if (failure is null)
                    throw new InvalidOperationException("Provider returned failure but error object is missing.");
                
                await HandleFailure(
                    attempt,
                    failure,
                    operation);
            }
            else
            {
                var response = result.Value!.ProviderPaymentId!;
                _stateMachine.WaitForReceipt(operation, response);
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

    private async Task HandleFailure(
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



    



        _logger.LogWarning(
            "Submission failed. Operation {OperationId}. Reason {Reason}. Retry after {Delay}",
            attempt.OperationId,
            failure.Reason,
            delay);
    }





 
}