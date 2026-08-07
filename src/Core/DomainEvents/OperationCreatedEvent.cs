using Core.DomainEvents;
namespace Core.DomainEvents;
public sealed record OperationCreatedEvent(
    string OperationId,
    decimal Amount,
    string Currency,
    DateTime OccurredOn)
    : IDomainEvent;