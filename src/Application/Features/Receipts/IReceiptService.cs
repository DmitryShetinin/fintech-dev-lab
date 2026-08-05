using Application.Common;
using Application.Receipts.Requests;

namespace Application.Receipts;

public interface IReceiptService
{
     Task<Result<bool>>  ProcessAsync(
        ReceiptRequest request,
        CancellationToken cancellationToken);
}