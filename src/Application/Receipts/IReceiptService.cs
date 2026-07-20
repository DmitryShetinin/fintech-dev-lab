using Application.Interface;
using Core.Models;

namespace Application.Receipts;

public sealed class OperationRepository : IOperationRepository
{
  private readonly AppDbContext _dbContext;

  public OperationRepository(AppDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public async Task AddAsync(
      Operation operation,
      CancellationToken cancellationToken)
  {
    await _dbContext.Operations.AddAsync(
        operation,
        cancellationToken);
  }

  public async Task<Operation?> GetByIdAsync(
      string operationId,
      CancellationToken cancellationToken)
  {
    return await _dbContext.Operations
        .Include(x => x.Events)
        .FirstOrDefaultAsync(
            x => x.OperationId == operationId,
            cancellationToken);
  }

  public async Task<bool> ExistsAsync(
      string operationId,
      CancellationToken cancellationToken)
  {
    return await _dbContext.Operations.AnyAsync(
        x => x.OperationId == operationId,
        cancellationToken);
  }

  public Task UpdateAsync(
      Operation operation,
      CancellationToken cancellationToken)
  {
    _dbContext.Operations.Update(operation);

    return Task.CompletedTask;
  }

  public async Task<IReadOnlyList<OperationEvent>> GetEventsAsync(
      string operationId,
      CancellationToken cancellationToken)
  {
    return await _dbContext.OperationEvents
        .Where(x => x.OperationId == operationId)
        .OrderBy(x => x.EventId)
        .ToListAsync(cancellationToken);
  }
}

