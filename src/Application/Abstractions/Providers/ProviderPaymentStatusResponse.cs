using Core.Enums;

namespace Application.Abstractions.Providers;

public sealed class ProviderPaymentStatusResponse
{
  public string ProviderPaymentId { get; init; }

  public ProviderPaymentStatus Status { get; init; }
}
