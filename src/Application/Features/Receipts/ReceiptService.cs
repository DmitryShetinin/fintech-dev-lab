using Application.Receipts.Responses;
using Application.Extensions;
using Application.Interface;
using Application.Common;
using Application.Receipts.Requests;
using Core.Models;








namespace Application.Receipts;


public sealed class ReceiptService : IReceiptService
{
  private readonly IOperationRepository _repository;
  private readonly OperationStateMachine _stateMachine;
  private readonly IUnitOfWork _unitOfWork;

  public ReceiptService(
      IOperationRepository repository,
      OperationStateMachine stateMachine,
      IUnitOfWork unitOfWork)
  {
    _repository = repository;
    _stateMachine = stateMachine;
    _unitOfWork = unitOfWork;
  }
  public async Task<Result<ReceiptResponse>> ProcessAsync(
      ReceiptRequest receipt,
      CancellationToken cancellationToken)
  {
    var operation = await _repository.GetByIdAsync(
        receipt.OperationId,
        cancellationToken);


    if (operation is null)
    {
      return Result<ReceiptResponse>.Failure(
          "Operation not found");
    }



    // Повторная квитанция
    if (operation.ProviderPaymentId is not null &&
        operation.ProviderPaymentId == receipt.ProviderPaymentId)
    {
      return Result<ReceiptResponse>.Success(
          new ReceiptResponse());
    }



    // Конфликт providerPaymentId
    if (operation.ProviderPaymentId is not null &&
        operation.ProviderPaymentId != receipt.ProviderPaymentId)
    {
      return Result<ReceiptResponse>.Failure(
          "Provider payment id mismatch");
    }



    // Первый раз устанавливаем связь
    operation.SetProviderPaymentId(
        receipt.ProviderPaymentId);



    switch (receipt.Result)
    {
      case ReceiptResult.COMPLETED:

        operation.Complete(
            _stateMachine);

        break;


      case ReceiptResult.REJECTED:

        operation.Reject(
            _stateMachine);

        break;
    }



    await _unitOfWork.SaveChangesAsync(
        cancellationToken);



    return Result<ReceiptResponse>.Success(
        new ReceiptResponse());
  }
}
