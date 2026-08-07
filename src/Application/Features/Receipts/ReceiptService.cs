
using Application.Abstractions.Persistence;
using Application.Abstractions.Telemetry;
using Application.Common;
using Application.Common.Failures;
using Application.Interface;
using Application.Receipts;
using Application.Receipts.Requests;
using Core.Enums;
using Core.Models;
using Microsoft.Extensions.Logging;

public sealed class ReceiptService : IReceiptService
{
    private readonly IOperationRepository _operationRepository;
    
    private readonly IUnitOfWork _unitOfWork;
    private readonly OperationStateMachine _stateMachine;
    private readonly ILogger<ReceiptService> _logger;
    private readonly IOperationMetrics _operationMetrics; 

    public ReceiptService(
        IOperationRepository operationRepository,
        IUnitOfWork unitOfWork,
        OperationStateMachine stateMachine, 
        ILogger<ReceiptService> logger, 
        IOperationMetrics operationMetrics)
    {
        _operationRepository = operationRepository;
        _unitOfWork = unitOfWork;
        _stateMachine = stateMachine;
        _logger = logger;
        _operationMetrics = operationMetrics; 
    }


    public async Task<Result<bool>> ProcessAsync(
        ReceiptRequest request,
        CancellationToken cancellationToken)
    {
        var operation =
            await _operationRepository.GetByIdAsync(
                request.OperationId,
                cancellationToken);


        if (operation is null)
        {
            return Result<bool>.Failure(
                new ApplicationFailure(
                    $"Operation {request.OperationId} not found"));
        }





        // повторная квитанция
        if (operation.Status == OperationStatus.Completed ||
            operation.Status == OperationStatus.Rejected)
        {
            _logger.LogInformation(
                "Duplicate receipt ignored. Operation {OperationId}",
                operation.Id);
            return Result<bool>.Success(true);
        }



        await _unitOfWork.ExecuteInTransactionAsync(
      ct =>
      {
          operation.SetProviderPaymentId(
              request.ProviderPaymentId);

          switch (request.Result)
          {
              case ReceiptResult.COMPLETED:
                  _stateMachine.Complete(operation);
                  _operationMetrics.AddOperationCompleted(); 
                  break;

              case ReceiptResult.REJECTED:
                  _stateMachine.Reject(operation);
                  _operationMetrics.AddOperationRejected(); 
                  break;
          }

          return Task.CompletedTask;
      },
      cancellationToken);





        return Result<bool>.Success(true);
    }
}