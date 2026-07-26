using Application.Abstractions.Providers;

namespace Application.Abstractions.Retry;



public interface IRetryPolicy
{
  TimeSpan GetRetryDelay(int retryCount);
}
