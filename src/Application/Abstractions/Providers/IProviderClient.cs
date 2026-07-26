using Application.Common;
using Application.Provider;

namespace Application.Abstractions.Providers;



public interface IProviderClient
{
  Task<Result<ProviderResponse>> CreatePaymentAsync(
      ProviderRequest request,
      CancellationToken cancellationToken);

  bool IsTransientFailure(ProviderResponse response);


}


