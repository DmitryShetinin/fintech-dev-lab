namespace Application.Operations.Requests;

public sealed class CreateOperationRequest
{
  public string OperationId { get; init; } = default!;

  public decimal Amount { get; init; }

  public string Currency { get; init; } = default!;

  public string Description { get; init; } = default!;
}
