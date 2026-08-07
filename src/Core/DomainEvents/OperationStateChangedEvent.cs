
 
using Core.Enums;

namespace Core.DomainEvents;

public sealed record OperationStateChangedEvent(
    string OperationId,
    OperationStatus? From,
    OperationStatus To,
    string Message,
    DateTime OccurredOn)
    : IDomainEvent;

    