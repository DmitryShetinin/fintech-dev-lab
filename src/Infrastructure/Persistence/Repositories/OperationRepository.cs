using Application.Interface;
using Core.Enums;
using Core.Models;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;


namespace Infrastructure.Persistence.Repositories;

public sealed class OperationRepository : IOperationRepository
{
  private readonly AppDbContext _dbContext;

  public OperationRepository(AppDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public async Task AddEventAsync(
      OperationEvent operationEvent,
      CancellationToken cancellationToken)
  {
    await _dbContext.OperationEvents
        .AddAsync(
            operationEvent,
            cancellationToken);
  }
  public async Task AddAsync(
      Operation operation,
      CancellationToken cancellationToken)
  {
    await _dbContext.Operations
        .AddAsync(operation, cancellationToken);
  }


  public async Task<Operation?> GetByIdAsync(
      string operationId,
      CancellationToken cancellationToken)
  {
    return await _dbContext.Operations
        .Include(x => x.Events)
        .FirstOrDefaultAsync(
            x => x.Id == operationId,
            cancellationToken);
  }


  public async Task<bool> ExistsAsync(
      string operationId,
      CancellationToken cancellationToken)
  {
    return await _dbContext.Operations
        .AnyAsync(
            x => x.Id == operationId,
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
        .AsNoTracking()
        .Where(x => x.OperationId == operationId)
        .OrderBy(x => x.EventId)
        .ToListAsync(cancellationToken);
  }


  public async Task<List<Operation>> GetProcessingAsync(
      DateTime now,
      CancellationToken cancellationToken)
  {
    return await _dbContext.Operations
        .AsNoTracking()
        .Where(x =>
            x.Status == OperationStatus.Processing &&
            (x.NextRetryAt == null || x.NextRetryAt <= now))
        .ToListAsync(cancellationToken);
  }



}
