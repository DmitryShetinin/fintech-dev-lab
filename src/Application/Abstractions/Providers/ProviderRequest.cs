namespace Application.Provider;


public sealed class ProviderRequest
{
    public string OperationId { get; init; } = null!;

    public string Amount { get; init; } = null!;

    public string Currency { get; init; } = null!;
}