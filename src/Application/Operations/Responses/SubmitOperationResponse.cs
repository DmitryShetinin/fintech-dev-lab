using Core.Enums;

namespace Application.Operations.Responses;

public sealed class SubmitOperationResponse
{
  public string OperationId { get; init; } = default!;

  public OperationStatus Status { get; init; }

  public string? ProviderPaymentId { get; init; }
}
