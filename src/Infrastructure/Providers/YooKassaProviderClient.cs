using System.Net.Http.Json;
using System.Text.Json;
using Application.Abstractions.Providers;
using Application.Common;
using Application.Common.Failures;
using Application.Provider;
using Core.Enums;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Providers;

public class YooKassaProvider : ProviderClientBase, IProviderClient
{
    private readonly HttpClient _httpClient;
    private readonly ProviderFailureClassifier _failureClassifier;
    private readonly ILogger<YooKassaProvider> _logger;

    public YooKassaProvider(
        HttpClient httpClient,
        ProviderFailureClassifier failureClassifier,
        ILogger<YooKassaProvider> logger)
    {
        _httpClient = httpClient;
        _failureClassifier = failureClassifier;
        _logger = logger;
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

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        request.Content = JsonContent.Create(
            payment,
            options: options);

        _logger.LogInformation(
            "Sending POST /payments to provider. OperationId={OperationId}",
            payment.OperationId);

        var response =
            await _httpClient.SendAsync(
                request,
                cancellationToken);

        _logger.LogInformation(
            "Provider response received. OperationId={OperationId}, StatusCode={StatusCode}",
            payment.OperationId,
            response.StatusCode);

        // TEST:
        // Имитируем потерю HTTP-ответа / зависший запрос.
        // Provider уже принял payment, но Candidate ещё не получил
        // окончательный результат CreatePaymentAsync.
        //
        // SubmissionProcessor имеет timeout 30 секунд,
        // поэтому этот Delay должен быть отменён раньше.
        await Task.Delay(
            TimeSpan.FromSeconds(40),
            cancellationToken);

        var body =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return Result<ProviderResponse>.Failure(
                new ProviderFailure(
                    _failureClassifier.Classify(
                        response.StatusCode),
                    $"Provider returned {(int)response.StatusCode}"));
        }

        var providerResponse =
            await response.Content.ReadFromJsonAsync<ProviderResponse>(
                cancellationToken);

        if (providerResponse is null)
        {
            return Result<ProviderResponse>.Failure(
                new ProviderFailure(
                    _failureClassifier.Classify(
                        response.StatusCode),
                    "Provider returned empty response."));
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
            providerPaymentId,
            "");

        var response =
            await _httpClient.SendAsync(
                request,
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return Result<ProviderPaymentStatusResponse>.Failure(
                new ProviderFailure(
                    _failureClassifier.Classify(
                        response.StatusCode),
                    $"Provider returned {(int)response.StatusCode}"));
        }

        var providerResponse =
            await response.Content.ReadFromJsonAsync<ProviderPaymentStatusResponse>(
                cancellationToken);

        if (providerResponse is null)
        {
            return Result<ProviderPaymentStatusResponse>.Failure(
                new ProviderFailure(
                    _failureClassifier.Classify(
                        response.StatusCode),
                    "Provider returned empty response."));
        }

        return Result<ProviderPaymentStatusResponse>.Success(
            providerResponse);
    }
}