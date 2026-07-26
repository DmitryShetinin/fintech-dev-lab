using Application.Abstractions.Providers;

namespace Application.Abstractions.Retry;

public sealed class ExponentialBackoffRetryPolicy : IRetryPolicy
{
  private readonly Random _random = new();

  private static readonly TimeSpan MaxDelay =
      TimeSpan.FromMinutes(5);


  public TimeSpan GetRetryDelay(int retryCount)
  {
    var exponentialSeconds =
        Math.Pow(2, retryCount);


    var limitedSeconds =
        Math.Min(
            exponentialSeconds,
            MaxDelay.TotalSeconds);


    var jitterMilliseconds =
        _random.Next(0, 1000);


    return TimeSpan.FromSeconds(limitedSeconds)
        + TimeSpan.FromMilliseconds(jitterMilliseconds);
  }
}
