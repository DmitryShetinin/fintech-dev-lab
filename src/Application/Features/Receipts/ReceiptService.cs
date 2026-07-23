using Application.Receipts.Responses;
using Application.Extensions;
using Application.Interface;
using Application.Common;








namespace Application.Receipts;


public sealed class ReceiptService : IReceiptService
{
  private readonly IOperationRepository _repository;


  public ReceiptService(
      IOperationRepository repository)
  {
    _repository = repository;
  }


  public async Task<Result<ReceiptResponse>> GetAsync(
      string operationId,
      CancellationToken cancellationToken)
  {
    var operation = await _repository.GetByIdAsync(
        operationId,
        cancellationToken);


    if (operation is null)
    {
      return Result<ReceiptResponse>.Failure(
          "Operation not found");
    }


    var response = operation.ToReceiptResponse();


    return Result<ReceiptResponse>.Success(response);
  }
}
