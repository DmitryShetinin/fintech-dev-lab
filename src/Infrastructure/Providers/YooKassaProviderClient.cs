using System.Net;
using System.Net.Http.Json;
using Application.Abstractions.Providers;
using Application.Common;
using Application.Provider;
using Core.Enums;

namespace Infrastructure.Providers;

public class YooKassaProvider : ProviderClientBase, IProviderClient
{

  private readonly HttpClient _httpClient;

  public YooKassaProvider(HttpClient httpClient)
  {
    _httpClient = httpClient;
  }

  public async Task<Result<ProviderResponse>> CreatePaymentAsync(
      ProviderRequest payment,
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
      return Result<ProviderResponse>.Failure(
          $"Provider returned {(int)response.StatusCode}");
    }

    var providerResponse = await response.Content.ReadFromJsonAsync<ProviderResponse>(cancellationToken);

    if (providerResponse is null)
    {
      return Result<ProviderResponse>.Failure(
          "Provider returned empty response.");
    }

    return Result<ProviderResponse>.Success(
        providerResponse);
  }

  public bool IsTransientFailure(ProviderResponse response) => response.HttpStatusCode switch
  {
    HttpStatusCode.TooManyRequests
        or HttpStatusCode.InternalServerError
        or HttpStatusCode.BadGateway
        or HttpStatusCode.ServiceUnavailable
        or HttpStatusCode.GatewayTimeout
            => true,

    _ => false

  };



}
