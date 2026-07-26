using Application.Common;
using Application.Provider;

namespace Application.Abstractions.Providers;



public interface IProviderClient
{
  Task<ProviderPaymentResponse> CreatePaymentAsync(
      ProviderPaymentRequest request,
      CancellationToken cancellationToken);

  RetryDecision GetRetryDecision(
      ProviderPaymentResponse response,
      int retryCount);
}


public sealed record RetryDecision(
    bool ShouldRetry,
    TimeSpan Delay)
{
  public static RetryDecision Retry(TimeSpan delay)
      => new(true, delay);

  public static RetryDecision NoRetry()
      => new(false, TimeSpan.Zero);
}
