using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;


namespace IntegrationTests.Operations;

public sealed class OperationConcurrencyTests
    : IClassFixture<WebApplicationFactory<Program>>
{
  private readonly HttpClient _client;

  public OperationConcurrencyTests(
      WebApplicationFactory<Program> factory)
  {
    _client = factory.CreateClient();
  }

  [Theory]
  [InlineData(10)]
  [InlineData(100)]
  [InlineData(500)]
  [InlineData(1000)]
  public async Task CreateOperation_ConcurrentRequests_ShouldCreateOnlyOneOperation(
        int requestCount)
  {
    // Arrange
    var request = new
    {
      operationId = Guid.NewGuid().ToString(),
      amount = 1000.00m,
      currency = "RUB",
      description = "Concurrency test"
    };

    // Act
    var tasks = Enumerable
        .Range(0, requestCount)
        .Select(_ => _client.PostAsJsonAsync(
            "/operations",
            request));

    var responses = await Task.WhenAll(tasks);

    // Assert
    responses.Count(x => x.StatusCode == HttpStatusCode.Created)
        .Should().Be(1);

    responses.Count(x => x.StatusCode == HttpStatusCode.Conflict)
        .Should().Be(requestCount - 1);
  }


  [Fact]
  public async Task CreateOperation_DuplicateOperation_ShouldReturnConflict()
  {
    var request = new
    {
      operationId = Guid.NewGuid().ToString(),
      amount = 1000m,
      currency = "RUB",
      description = "Test"
    };

    var first = await _client.PostAsJsonAsync(
        "/operations",
        request);

    var second = await _client.PostAsJsonAsync(
        "/operations",
        request);

    first.StatusCode.Should().Be(HttpStatusCode.Created);
    second.StatusCode.Should().Be(HttpStatusCode.Conflict);
  }


  [Fact]
  public async Task Submit_ShouldMoveOperationToProcessing()
  {
    var operationId = Guid.NewGuid().ToString();

    await _client.PostAsJsonAsync(
        "/operations",
        new
        {
          operationId,
          amount = 1000m,
          currency = "RUB",
          description = "Test"
        });

    var submit = await _client.PostAsync(
        $"/operations/{operationId}/submit",
        null);

    submit.StatusCode.Should().Be(HttpStatusCode.OK);

    var operation = await _client.GetAsync(
        $"/operations/{operationId}");

    var body = await operation.Content.ReadAsStringAsync();

    body.Should().Contain("Processing");
  }


  [Fact]
  public async Task Submit_NotExistingOperation_ShouldReturn404()
  {
    var response = await _client.PostAsync(
        $"/operations/{Guid.NewGuid()}/submit",
        null);

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
  }
}
