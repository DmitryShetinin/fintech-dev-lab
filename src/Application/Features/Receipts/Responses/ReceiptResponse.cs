using Core.Enums;

namespace Application.Receipts.Responses;

public class ReceiptResponse
{
  public string OperationId { get; set; } = null!;

  public decimal Amount { get; set; }

  public string Currency { get; set; } = null!;

  public string Description { get; set; } = null!;

  public OperationStatus Status { get; set; }

  public DateTime CreatedAt { get; set; }

  public string? ProviderPaymentId { get; set; }
}
