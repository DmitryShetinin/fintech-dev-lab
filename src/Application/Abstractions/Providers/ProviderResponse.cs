
using System.Net;
using Core.Enums;

namespace Application.Provider;


public sealed record ProviderResponse
{
  public string? ProviderPaymentId { get; init; }

  public HttpStatusCode? HttpStatusCode { get; init; }

  public ProviderFailureReason ProviderFailureReason { get; init; } =
      ProviderFailureReason.None;
}
