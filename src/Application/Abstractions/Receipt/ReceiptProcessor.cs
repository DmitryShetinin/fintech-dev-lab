using Application.Abstractions.Providers;
using Application.Common;
using Application.Interface;
using Application.Receipts.Requests;
using Application.Receipts.Responses;
using Core.Enums;
using Core.Models;

namespace Application.Abstractions.Receipt;


public class ReceiptProcessor : IReceiptProcessor
{
  private readonly IOperationRepository _operationRepository;
  private readonly IProviderClientFactory _providerFactory;
  private readonly IUnitOfWork _unitOfWork;
  private readonly OperationStateMachine _stateMachine;


  public async Task ProcessAsync(
      CancellationToken token)
  {
    var operations =
        await _operationRepository
            .GetWaitingForReceiptAsync(token);


    foreach (var operation in operations)
    {
      await ProcessOperationAsync(
          operation,
          token);
    }
  }

  public Task<Result<ReceiptResponse>> ProcessAsync(ReceiptRequest receipt, CancellationToken token)
  {
    throw new NotImplementedException();
  }

  private async Task ProcessOperationAsync(
    Operation operation,
    CancellationToken token)
  {
    var provider =
        _providerFactory.Get(operation.Provider);


    var result =
        await provider.GetPaymentStatusAsync(
            operation.ProviderPaymentId!,
            token);


    if (!result.IsSuccess)
    {
      // тут позже будет retry для receipt
      return;
    }


    var response = result.Value!;


    switch (response.Status)
    {
      case ProviderPaymentStatus.Succeeded:

        operation.Complete(
            _stateMachine);

        break;


      case ProviderPaymentStatus.Failed:

        operation.Reject(
            _stateMachine);

        break;


      case ProviderPaymentStatus.Pending:

        break;
    }


    await _unitOfWork.SaveChangesAsync(token);
  }
}
