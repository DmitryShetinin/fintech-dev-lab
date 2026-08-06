 
using Application.Common;
using Application.Common.Failures;
using Application.Interface;
using Application.Receipts;
using Application.Receipts.Requests;
using Core.Enums;
using Core.Models;

public sealed class ReceiptService : IReceiptService
{
    private readonly IOperationRepository _operationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly OperationStateMachine _stateMachine;


    public ReceiptService(
        IOperationRepository operationRepository,
        IUnitOfWork unitOfWork,
        OperationStateMachine stateMachine)
    {
        _operationRepository = operationRepository;
        _unitOfWork = unitOfWork;
        _stateMachine = stateMachine;
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


        if (operation.ProviderPaymentId is not null &&
            operation.ProviderPaymentId != request.ProviderPaymentId)
        {
            return Result<bool>.Failure(
                new ApplicationFailure(
                    "ProviderPaymentId mismatch"));
        }


        operation.SetProviderPaymentId(
            request.ProviderPaymentId);



        // повторная квитанция
        if (operation.Status == OperationStatus.Completed ||
            operation.Status == OperationStatus.Rejected)
        {
            return Result<bool>.Success(true);
        }



        switch (request.Result)
        {
            case ReceiptResult.COMPLETED:

            
                _stateMachine.Complete(operation);

                break;


            case ReceiptResult.REJECTED:

         
                _stateMachine.Reject(operation);
                break;
        }



        await _unitOfWork.SaveChangesAsync(
            cancellationToken);


        return Result<bool>.Success(true);
    }
}