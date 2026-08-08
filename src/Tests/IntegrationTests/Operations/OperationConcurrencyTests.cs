using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IntegrationTests.Operations;

public sealed class OperationConcurrencyTests
: IClassFixture<WebApplicationFactory<Program>>
{
private readonly HttpClient _client;
 
public OperationConcurrencyTests(WebApplicationFactory<Program> factory)
{
    _client = factory.CreateClient();
}

// ============================================================
// CREATE
// ============================================================

[Fact]
public async Task CreateOperation_ShouldReturnCreated()
{
    var request = CreateRequest();

    var response = await _client.PostAsJsonAsync(
        "/operations",
        request);

    response.StatusCode.Should().Be(HttpStatusCode.Created);

    var operation =
        await response.Content.ReadFromJsonAsync<JsonElement>();

    operation.GetProperty("operationId")
        .GetString()
        .Should()
        .Be(request.OperationId);

    operation.GetProperty("amount")
        .GetDecimal()
        .Should()
        .Be(request.Amount);

    operation.GetProperty("currency")
        .GetString()
        .Should()
        .Be(request.Currency);

    operation.GetProperty("description")
        .GetString()
        .Should()
        .Be(request.Description);
}

[Fact]
public async Task CreateOperation_DuplicateOperation_ShouldReturnBadRequest()
{
    var request = CreateRequest();

    var firstResponse = await _client.PostAsJsonAsync(
        "/operations",
        request);

    firstResponse.StatusCode
        .Should()
        .Be(HttpStatusCode.Created);

    var secondResponse = await _client.PostAsJsonAsync(
        "/operations",
        request);

    secondResponse.StatusCode
        .Should()
        .Be(HttpStatusCode.BadRequest);
}

[Fact]
public async Task CreateOperation_NegativeAmount_ShouldReturnBadRequest()
{
    var request = new
    {
        operationId = Guid.NewGuid().ToString(),
        amount = -100m,
        currency = "RUB",
        description = "Invalid amount"
    };

    var response = await _client.PostAsJsonAsync(
        "/operations",
        request);

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
}

[Fact]
public async Task CreateOperation_NonRubCurrency_ShouldReturnBadRequest()
{
    var request = new
    {
        operationId = Guid.NewGuid().ToString(),
        amount = 100m,
        currency = "USD",
        description = "Invalid currency"
    };

    var response = await _client.PostAsJsonAsync(
        "/operations",
        request);

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
}


[Fact]
public async Task CreateOperation_InvalidRequest_ShouldReturnBadRequest()
{
    var request = new
    {
        operationId = Guid.NewGuid().ToString(),
        amount = -100m,
        currency = "RUB",
        description = ""
    };

    var response = await _client.PostAsJsonAsync(
        "/operations",
        request);

    response.StatusCode
        .Should()
        .Be(HttpStatusCode.BadRequest);
}

// ============================================================
// CREATE CONCURRENCY
// ============================================================

[Fact]
public async Task CreateOperation_ConcurrentRequestsWithSameId_ShouldCreateOnlyOne()
{
    var request = CreateRequest();

    const int requestCount = 10;

    var tasks = Enumerable
        .Range(0, requestCount)
        .Select(_ =>
            _client.PostAsJsonAsync(
                "/operations",
                request));

    var responses = await Task.WhenAll(tasks);

    var created = responses
        .Count(x => x.StatusCode == HttpStatusCode.Created);

    var failed = responses
        .Count(x => x.StatusCode == HttpStatusCode.BadRequest);

    created.Should().Be(1);

    var statusCodes = responses
    .GroupBy(x => x.StatusCode)
    .Select(g => $"{g.Key}: {g.Count()}");

    Console.WriteLine(
        string.Join(", ", statusCodes));

    responses.Count(x => x.StatusCode == HttpStatusCode.Created)
        .Should().Be(1);
}

[Fact]
public async Task CreateOperation_ConcurrentRequestsWithDifferentIds_ShouldCreateAll()
{
    const int requestCount = 10;

    var requests = Enumerable
        .Range(0, requestCount)
        .Select(_ => CreateRequest())
        .ToArray();

    var tasks = requests.Select(request =>
        _client.PostAsJsonAsync(
            "/operations",
            request));

    var responses = await Task.WhenAll(tasks);

    responses.Should()
        .OnlyContain(x =>
            x.StatusCode == HttpStatusCode.Created);
}

// ============================================================
// GET
// ============================================================

[Fact]
public async Task GetOperation_Existing_ShouldReturnOk()
{
    var request = CreateRequest();

    var createResponse = await _client.PostAsJsonAsync(
        "/operations",
        request);

    createResponse.StatusCode
        .Should()
        .Be(HttpStatusCode.Created);

    var response = await _client.GetAsync(
        $"/operations/{request.OperationId}");

    response.StatusCode
        .Should()
        .Be(HttpStatusCode.OK);

    var operation =
        await response.Content.ReadFromJsonAsync<JsonElement>();

    operation.GetProperty("operationId")
        .GetString()
        .Should()
        .Be(request.OperationId);
}

[Fact]
public async Task GetOperation_NotExisting_ShouldReturnNotFound()
{
    var operationId = Guid.NewGuid().ToString();

    var response = await _client.GetAsync(
        $"/operations/{operationId}");

    response.StatusCode
        .Should()
        .Be(HttpStatusCode.NotFound);
}

// ============================================================
// SUBMIT
// ============================================================

[Fact]
public async Task Submit_ExistingOperation_ShouldReturnAccepted()
{
    var request = CreateRequest();

    var createResponse = await _client.PostAsJsonAsync(
        "/operations",
        request);

    createResponse.StatusCode
        .Should()
        .Be(HttpStatusCode.Created);

    var response = await _client.PostAsync(
        $"/operations/{request.OperationId}/submit",
        null);

    response.StatusCode
        .Should()
        .Be(HttpStatusCode.Accepted);
}

[Fact]
public async Task Submit_ShouldChangeStatusToProcessing()
{
    var request = CreateRequest();

    var createResponse = await _client.PostAsJsonAsync(
        "/operations",
        request);

    createResponse.StatusCode
        .Should()
        .Be(HttpStatusCode.Created);

    var submitResponse = await _client.PostAsync(
        $"/operations/{request.OperationId}/submit",
        null);

    submitResponse.StatusCode
        .Should()
        .Be(HttpStatusCode.Accepted);

    var getResponse = await _client.GetAsync(
        $"/operations/{request.OperationId}");

    getResponse.StatusCode
        .Should()
        .Be(HttpStatusCode.OK);

    var operation =
        await getResponse.Content.ReadFromJsonAsync<JsonElement>();

    // OperationStatus:
    // Created    = 0
    // Processing = 1
    operation.GetProperty("status")
        .GetInt32()
        .Should()
        .Be(1);
}

[Fact]
public async Task Submit_NotExistingOperation_ShouldReturnBadRequest()
{
    var operationId = Guid.NewGuid().ToString();

    var response = await _client.PostAsync(
        $"/operations/{operationId}/submit",
        null);

    response.StatusCode
        .Should()
        .Be(HttpStatusCode.BadRequest);
}

[Fact]
public async Task Submit_Twice_ShouldNotCorruptOperation()
{
    var request = CreateRequest();

    var createResponse = await _client.PostAsJsonAsync(
        "/operations",
        request);

    createResponse.StatusCode
        .Should()
        .Be(HttpStatusCode.Created);

    var firstSubmit = await _client.PostAsync(
        $"/operations/{request.OperationId}/submit",
        null);

    firstSubmit.StatusCode
        .Should()
        .Be(HttpStatusCode.Accepted);

    var secondSubmit = await _client.PostAsync(
        $"/operations/{request.OperationId}/submit",
        null);

    secondSubmit.StatusCode
        .Should()
        .BeOneOf(
            HttpStatusCode.Accepted,
            HttpStatusCode.BadRequest);

    var getResponse = await _client.GetAsync(
        $"/operations/{request.OperationId}");

    getResponse.StatusCode
        .Should()
        .Be(HttpStatusCode.OK);

    var operation =
        await getResponse.Content.ReadFromJsonAsync<JsonElement>();

    operation.GetProperty("status")
        .GetInt32()
        .Should()
        .Be(1);
}

// ============================================================
// SUBMIT CONCURRENCY
// ============================================================

[Fact]
public async Task Submit_ConcurrentRequests_ShouldNotCorruptOperation()
{
    var request = CreateRequest();

    var createResponse = await _client.PostAsJsonAsync(
        "/operations",
        request);

    createResponse.StatusCode
        .Should()
        .Be(HttpStatusCode.Created);

    const int requestCount = 10;

    var tasks = Enumerable
        .Range(0, requestCount)
        .Select(_ =>
            _client.PostAsync(
                $"/operations/{request.OperationId}/submit",
                null));

    var responses = await Task.WhenAll(tasks);

    responses.Should()
        .OnlyContain(response =>
            response.StatusCode == HttpStatusCode.Accepted ||
            response.StatusCode == HttpStatusCode.BadRequest);

    var getResponse = await _client.GetAsync(
        $"/operations/{request.OperationId}");

    getResponse.StatusCode
        .Should()
        .Be(HttpStatusCode.OK);

    var operation =
        await getResponse.Content.ReadFromJsonAsync<JsonElement>();

    operation.GetProperty("status")
        .GetInt32()
        .Should()
        .Be(1);
}

// ============================================================
// EVENTS
// ============================================================

[Fact]
public async Task GetEvents_ExistingOperation_ShouldReturnOk()
{
    var request = CreateRequest();

    var createResponse = await _client.PostAsJsonAsync(
        "/operations",
        request);

    createResponse.StatusCode
        .Should()
        .Be(HttpStatusCode.Created);

    var response = await _client.GetAsync(
        $"/operations/{request.OperationId}/events");

    response.StatusCode
        .Should()
        .Be(HttpStatusCode.OK);
}

[Fact]
public async Task GetEvents_NotExistingOperation_ShouldReturnNotFound()
{
    var operationId = Guid.NewGuid().ToString();

    var response = await _client.GetAsync(
        $"/operations/{operationId}/events");

    response.StatusCode
        .Should()
        .Be(HttpStatusCode.NotFound);
}

// ============================================================
// HELPERS
// ============================================================

private static CreateOperationRequestDto CreateRequest()
{
    return new CreateOperationRequestDto
    {
        OperationId = Guid.NewGuid().ToString(),
        Amount = 1000m,
        Currency = "RUB",
        Description = "Integration test"
    };
}

private sealed class CreateOperationRequestDto
{
    public string OperationId { get; init; } = null!;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = null!;
    public string Description { get; init; } = null!;
}
 

}
