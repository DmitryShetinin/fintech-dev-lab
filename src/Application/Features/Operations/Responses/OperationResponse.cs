



using Core.Enums;

namespace Application.Operations.Responses;

public sealed class OperationResponse
{
  public string OperationId { get; init; } = default!;

  public decimal Amount { get; init; }

  public string Currency { get; init; } = default!;

  public string Description { get; init; } = default!;

  public OperationStatus Status { get; init; }

  public string? ProviderPaymentId { get; init; }
}
