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

    var providerResponse = await response.Content.ReadFromJsonAsync<ProviderPaymentResponse>(cancellationToken);

    if (providerResponse is null)
    {
      return Result<ProviderPaymentResponse>.Failure(
          "Provider returned empty response.");
    }

    return Result<ProviderPaymentResponse>.Success(
        providerResponse);
  }

  public RetryDecision GetRetryDecision(ProviderPaymentResponse response, int retryCount)
  {
    var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));

    if (response.ProviderFailureReason is
       ProviderFailureReason.Timeout
       or ProviderFailureReason.Network
       or ProviderFailureReason.Dns)
    {
      return RetryDecision.Retry(delay);
    }

    return response.HttpStatusCode switch
    {
      HttpStatusCode.TooManyRequests
          or HttpStatusCode.InternalServerError
          or HttpStatusCode.BadGateway
          or HttpStatusCode.ServiceUnavailable
          or HttpStatusCode.GatewayTimeout
              => RetryDecision.Retry(delay),

      _ => RetryDecision.NoRetry()
    };
  }



}
