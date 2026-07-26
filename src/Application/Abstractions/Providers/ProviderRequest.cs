namespace Application.Provider;


public sealed class ProviderRequest
{
  public string OperationId { get; init; } = null!;

  public decimal Amount { get; init; }

  public string Currency { get; init; } = null!;

  public string Description { get; init; } = null!;
}
