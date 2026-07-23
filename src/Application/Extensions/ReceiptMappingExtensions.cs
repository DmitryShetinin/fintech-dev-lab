using Application.Receipts.Responses;
using Core.Models;

namespace Application.Extensions;


public static class ReceiptMappingExtensions
{
  public static ReceiptResponse ToReceiptResponse(
      this Operation operation)
  {
    return new ReceiptResponse
    {
      OperationId = operation.OperationId,
      Amount = operation.Amount,
      Currency = operation.Currency,
      Description = operation.Description,
      Status = operation.Status,
      ProviderPaymentId = operation.ProviderPaymentId
    };
  }
}

