
using System.Net;
using Core.Enums;

namespace Application.Provider;


public sealed class ProviderPaymentResponse
{

  public bool IsSuccess { get; init; }

  public HttpStatusCode? HttpStatusCode { get; init; }

  public ProviderFailureReason ProviderFailureReason { get; init; }

  public ProviderPaymentResponse? Payment { get; init; }
}
