using Application.Common;
using Application.Receipts.Requests;
using Application.Receipts.Responses;

namespace Application.Abstractions.Receipt;

public interface IReceiptProcessor
{
  Task<Result<ReceiptResponse>> ProcessAsync(
    ReceiptRequest receipt,
    CancellationToken token);
}
