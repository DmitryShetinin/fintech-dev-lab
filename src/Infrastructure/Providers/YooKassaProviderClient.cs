using System.Net.Http.Json;
using Application.Abstractions.Providers;
using Application.Common;
using Application.Provider;

namespace Infrastructure.Providers;

public class YooKassaProvider : ProviderClientBase, IProviderClient
{

  private readonly HttpClient _httpClient;

  public YooKassaProvider(HttpClient httpClient)
  {
    _httpClient = httpClient;
  }

  public async Task<Result<ProviderPaymentResponse>> CreatePaymentAsync(
      ProviderPaymentRequest payment,
      CancellationToken cancellationToken)
  {
    using var request = CreateRequest(
            HttpMethod.Post,
            "/payments",
            payment.OperationId,
            payment);

    var response =
        await _httpClient.SendAsync(
            request,
            cancellationToken);

    if (!response.IsSuccessStatusCode)
    {
      return Result<ProviderPaymentResponse>.Failure(
          $"Provider returned {(int)response.StatusCode}");
    }

    var providerResponse =
        await response.Content.ReadFromJsonAsync<ProviderPaymentResponse>(
            cancellationToken: cancellationToken);

    if (providerResponse is null)
    {
      return Result<ProviderPaymentResponse>.Failure(
          "Provider returned empty response.");
    }

    return Result<ProviderPaymentResponse>.Success(
        providerResponse);
  }
}
