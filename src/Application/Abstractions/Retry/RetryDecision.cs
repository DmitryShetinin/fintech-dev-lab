using Application.Abstractions.Providers;
using Application.Configuration;
using Core.Enums;
using Microsoft.Extensions.Options;

namespace Application.Abstractions.Retry;

public sealed class ExponentialBackoffRetryPolicy : IRetryPolicy
{
  private readonly RetryOptions _options;


  public ExponentialBackoffRetryPolicy(
      IOptions<RetryOptions> options)
  {
    _options = options.Value;
  }


  public bool CanRetry(
       ProviderFailureReason reason,
       int retryCount)
  {
    if (retryCount >= _options.MaxAttempts)
      return false;
    
    return reason switch
    {
      ProviderFailureReason.Network => true,

      ProviderFailureReason.Timeout => true,

      ProviderFailureReason.Dns => true,

      ProviderFailureReason.HttpTransient => true,

      ProviderFailureReason.HttpPermanent => false,

      ProviderFailureReason.Validation => false,

      ProviderFailureReason.Unauthorized => false,

      _ => false
    };
  }

  public TimeSpan GetRetryDelay(int retryCount)
  {
    var exponentialSeconds = _options.InitialDelaySeconds * Math.Pow(2, retryCount);


    var cappedSeconds =
        Math.Min(
            exponentialSeconds,
            _options.MaxDelaySeconds);


    var jitter =
        Random.Shared.NextDouble() * cappedSeconds;


    return TimeSpan.FromSeconds(jitter);
  }
}
