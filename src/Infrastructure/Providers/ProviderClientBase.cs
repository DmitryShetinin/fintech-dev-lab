using System.Net.Http.Json;

namespace Infrastructure.Providers;


public abstract class ProviderClientBase
{
  protected HttpRequestMessage CreateRequest<T>(
      HttpMethod method,
      string uri,
      string operationId,
      T body)
  {
    var request = new HttpRequestMessage(method, uri);

    request.Headers.Add("Idempotency-Key", operationId);
    request.Headers.Add("X-Correlation-ID", operationId);

    request.Content = JsonContent.Create(body);

    return request;
  }
}
