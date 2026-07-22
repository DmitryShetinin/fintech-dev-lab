
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
        OperationEvent operationEvent,
        CancellationToken cancellationToken);


  Task<bool> ExistsAsync(
      string operationId,
      CancellationToken cancellationToken);

  Task<IReadOnlyList<OperationEvent>> GetEventsAsync(
      string operationId,
      CancellationToken cancellationToken);
}
