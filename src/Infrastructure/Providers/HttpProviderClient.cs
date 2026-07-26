using Application.Abstractions.Providers;
using Application.Common;
using Application.Provider;

namespace Infrastructure.Providers;

public sealed class HttpProviderClient : IProviderClient
{
  public Task<Result<ProviderResponse>> CreatePaymentAsync(
  ProviderRequest request,
  CancellationToken cancellationToken)
  {

    // HTTP запрос


    return Task.FromResult(
        Result<ProviderResponse>.Success(
            new ProviderResponse
            {
              ProviderPaymentId = "123"
            }));
  }

  public bool IsTransientFailure(ProviderResponse response)
  {
    throw new NotImplementedException();
  }
}
