namespace Application.Provider;


public sealed class ProviderPaymentRequest
{
  public required string OperationId { get; init; }

  public required string Amount { get; init; }

  public required string Currency { get; init; }
}
