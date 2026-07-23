using Application.Abstractions.Providers;
using Application.Common;
using Application.Provider;

namespace Infrastructure.Providers;

public sealed class HttpProviderClient : IProviderClient
{
  public Task<Result<ProviderPaymentResponse>> CreatePaymentAsync(
  ProviderPaymentRequest request,
  CancellationToken cancellationToken)
  {

    // HTTP запрос


    return Task.FromResult(
        Result<ProviderPaymentResponse>.Success(
            new ProviderPaymentResponse
            {
              ProviderPaymentId = "123"
            }));
  }


}
