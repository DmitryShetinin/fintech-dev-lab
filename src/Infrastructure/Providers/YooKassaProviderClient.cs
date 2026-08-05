using System.Net;
using System.Net.Http.Json;
using Application.Abstractions.Providers;
using Application.Common;
using Application.Common.Failures;
using Application.Provider;
using Core.Enums;

namespace Infrastructure.Providers;

public class YooKassaProvider : ProviderClientBase, IProviderClient
{

  private readonly HttpClient _httpClient;
  private readonly ProviderFailureClassifier _failureClassifier; 
  public YooKassaProvider(HttpClient httpClient, ProviderFailureClassifier failureClassifier)
  {
    _httpClient = httpClient;
    _failureClassifier = failureClassifier; 
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
            new ProviderFailure(
            _failureClassifier.Classify(response.StatusCode),
            $"Provider returned {(int)response.StatusCode}"
        ));
    }

    var providerResponse = await response.Content.ReadFromJsonAsync<ProviderResponse>(cancellationToken);

    if (providerResponse is null)
    {
      return Result<ProviderResponse>.Failure(
            new ProviderFailure(
            _failureClassifier.Classify(response.StatusCode),
            $"Provider returned empty response."
        ));
    }

    return Result<ProviderResponse>.Success(
        providerResponse);
  }

  public async Task<Result<ProviderPaymentStatusResponse>> GetPaymentStatusAsync(
    string providerPaymentId,
    CancellationToken cancellationToken)
  {
    using var request = CreateRequest(
        HttpMethod.Get,
        $"/payments/{providerPaymentId}",
        providerPaymentId, "");


    var response = await _httpClient.SendAsync(
        request,
        cancellationToken);


    if (!response.IsSuccessStatusCode)
    {


          return Result<ProviderPaymentStatusResponse>.Failure(
          new ProviderFailure(
              _failureClassifier.Classify(response.StatusCode!),
              $"Provider returned {(int)response.StatusCode}"
          ));

 


    }


    var providerResponse =
        await response.Content.ReadFromJsonAsync<ProviderPaymentStatusResponse>(
            cancellationToken);


    if (providerResponse is null)
    {
      return Result<ProviderPaymentStatusResponse>.Failure(
            new ProviderFailure(
            _failureClassifier.Classify(response.StatusCode),
            $"Provider returned empty response."
        ));

    }


    return Result<ProviderPaymentStatusResponse>.Success(
        providerResponse);
  }





 


}
