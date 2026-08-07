using Core.Models;

namespace Application.Abstractions.Persistence;

public interface IOperationEventRepository
{
  Task AddAsync(OperationHistory history, CancellationToken token);


}
