namespace Application.Receipts.Responses;



public sealed class ReceiptRequest
{
  public required string ProviderPaymentId { get; init; }

  public required string OperationId { get; init; }

  public required string Result { get; init; }

  public string? Message { get; init; }

  public DateTime OccurredAt { get; init; }
}

public enum ReceiptResult
{
  COMPLETED,
  REJECTED
}
