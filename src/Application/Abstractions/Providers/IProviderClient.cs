using Application.Common;
using Application.Provider;

namespace Application.Abstractions.Providers;



public interface IProviderClient
{
  Task<Result<ProviderResponse>> CreatePaymentAsync(
      ProviderRequest request,
      CancellationToken cancellationToken);

 

  Task<Result<ProviderPaymentStatusResponse>> GetPaymentStatusAsync(
      string providerPaymentId,
      CancellationToken cancellationToken);


}


