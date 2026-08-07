

using Application.Abstractions;
 
using Application.Abstractions.Persistence;
using Application.Interface;
using Core.DomainEvents;
using Core.Models;

namespace Application.DomainEvents;

public sealed class DomainEventDispatcher
    : IDomainEventDispatcher
{
    private readonly IOperationEventRepository _operationEventRepository;

    public DomainEventDispatcher(
        IOperationEventRepository operationEventRepository)
    {
        _operationEventRepository = operationEventRepository;
    }

    public async Task DispatchAsync(
        IEnumerable<IDomainEvent> events,
        CancellationToken cancellationToken)
    {
        foreach (var domainEvent in events)
        {
            switch (domainEvent)
            {
             
                case OperationStateChangedEvent stateChanged:

                    var history =
                        OperationHistory.Create(
                            stateChanged.OperationId,
                            stateChanged.From,
                            stateChanged.To,
                            stateChanged.Message);

                    await _operationEventRepository.AddAsync(
                        history,
                        cancellationToken);
                    
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unknown domain event: {domainEvent.GetType().Name}");
            }
        }
    }
}
