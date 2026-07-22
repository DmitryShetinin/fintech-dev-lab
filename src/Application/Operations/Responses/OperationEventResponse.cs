using Core.Enums;

namespace Application.Operations.Responses;

public sealed class OperationEventResponse
{
  public long EventId { get; init; }

  public string Type { get; init; } = default!;

  public OperationStatus? FromStatus { get; init; }

  public OperationStatus ToStatus { get; init; }

  public string Message { get; init; } = default!;

  public DateTime OccurredAt { get; init; }
}
