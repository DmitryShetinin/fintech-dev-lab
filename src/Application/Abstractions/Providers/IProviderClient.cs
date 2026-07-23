using Application.Common;
using Application.Provider;

namespace Application.Abstractions.Providers;


public interface IProviderClient
{
  Task<Result<ProviderPaymentResponse>> CreatePaymentAsync(
      ProviderPaymentRequest request,
      CancellationToken cancellationToken);
}
