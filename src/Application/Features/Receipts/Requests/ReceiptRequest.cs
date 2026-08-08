
using System.Text.Json.Serialization;

namespace Application.Receipts.Requests;

public sealed class ReceiptRequest
{
  public required string ProviderPaymentId { get; init; }

  public required string OperationId { get; init; }

  public required ReceiptResult Result { get; init; }

  public string? Message { get; init; }

  public DateTime OccurredAt { get; init; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReceiptResult
{
  COMPLETED,
  REJECTED
}


