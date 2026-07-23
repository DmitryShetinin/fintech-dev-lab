namespace Application.Provider;


public sealed class ProviderPaymentResponse
{
  public string ProviderPaymentId { get; init; } = null!;

  public bool IsSuccess { get; init; }

  public string? Error { get; init; }
}
