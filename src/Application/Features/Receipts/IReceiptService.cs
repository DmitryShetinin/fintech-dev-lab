using Application.Common;
using Application.Receipts.Responses;


namespace Application.Receipts;

public interface IReceiptService
{
  Task<Result<ReceiptResponse>> GetAsync(
      string operationId,
      CancellationToken cancellationToken);
}
