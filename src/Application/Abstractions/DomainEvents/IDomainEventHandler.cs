

 
using Core.DomainEvents;

namespace Application.Abstractions;


public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    Task HandleAsync(
        TEvent domainEvent,
        CancellationToken cancellationToken);
}

