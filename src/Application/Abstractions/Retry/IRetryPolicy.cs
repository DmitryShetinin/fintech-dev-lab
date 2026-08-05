using Application.Abstractions.Providers;
using Core.Enums;

namespace Application.Abstractions.Retry;



public interface IRetryPolicy
{
  TimeSpan GetRetryDelay(int retryCount);

  bool CanRetry(
    ProviderFailureReason reason,
    int retryCount);
}
