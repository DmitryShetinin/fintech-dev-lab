namespace Application.Provider;
 

public sealed class ProviderPaymentResponse
{
    public required string ProviderPaymentId { get; init; }

    public required string Status { get; init; }
}