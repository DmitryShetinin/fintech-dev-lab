


 
using Core.DomainEvents;

namespace Application.Abstractions;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(
        IEnumerable<IDomainEvent> events,
        CancellationToken cancellationToken);
}