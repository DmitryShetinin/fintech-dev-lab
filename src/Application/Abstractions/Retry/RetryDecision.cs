using Application.Abstractions.Providers;
using Application.Configuration;
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


  public bool CanRetry(int retryCount)
  {
    return retryCount < _options.MaxAttempts;
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
