
using Core.Models;

namespace Application.Interface;



public interface IOperationRepository
{
  Task<Operation?> GetByIdAsync(
      string operationId,
      CancellationToken cancellationToken);

  Task AddAsync(
      Operation operation,
      CancellationToken cancellationToken);

  Task UpdateAsync(
      Operation operation,
      CancellationToken cancellationToken);


  Task AddEventAsync(
        OperationHistory operationEvent,
        CancellationToken cancellationToken);

 


  Task<IReadOnlyList<Operation>> GetReadyForRetryAsync(CancellationToken cancellationToken);


  Task<IReadOnlyList<OperationHistory>> GetEventsAsync(
      string operationId,
      CancellationToken cancellationToken);

  Task<IReadOnlyList<Operation>> GetWaitingForReceiptAsync(
      CancellationToken cancellationToken);
}

