using Application.Abstractions.Providers;

namespace Application.Abstractions.Retry;

public sealed class ExponentialBackoffRetryPolicy : IRetryPolicy
{
  private const int MaxAttempts = 10;

  private static readonly TimeSpan MaxDelay =
      TimeSpan.FromMinutes(5);


  public bool CanRetry(int retryCount)
  {
    return retryCount < MaxAttempts;
  }


  public TimeSpan GetRetryDelay(int retryCount)
  {
    var exponentialSeconds =
        Math.Pow(2, retryCount);


    var cappedSeconds =
        Math.Min(
            exponentialSeconds,
            MaxDelay.TotalSeconds);


    var jitter =
        Random.Shared.NextDouble() * cappedSeconds;


    return TimeSpan.FromSeconds(jitter);
  }
}
