using Application.Abstractions.Providers;
using Application.Common;
using Application.Provider;

namespace Infrastructure.Providers;

public class YooKassaProvider : IProviderClient
{
  public Task<Result<ProviderPaymentResponse>> CreatePaymentAsync(
    ProviderPaymentRequest request,
    CancellationToken cancellationToken)
  {

    // HTTP запрос в ЮKassa



    return Task.FromResult(
        Result<ProviderPaymentResponse>.Success(
            new ProviderPaymentResponse
            {
              ProviderPaymentId = "123"
            }));
  }



}
