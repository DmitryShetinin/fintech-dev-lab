using Application.Common;
using Application.Provider;

namespace Application.Abstractions.Providers;



public interface IProviderClient
{
  Task<Result<ProviderResponse>> CreatePaymentAsync(
      ProviderRequest request,
      CancellationToken cancellationToken);

  bool IsTransientFailure(ProviderResponse response);

  Task<Result<ProviderPaymentStatusResponse>> GetPaymentStatusAsync(
      string providerPaymentId,
      CancellationToken cancellationToken);


}


