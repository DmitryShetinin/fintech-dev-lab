using Application.Interface;
using Core.Enums;
using Core.Models;
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
        OperationHistory operationEvent,
        CancellationToken cancellationToken)
    {
        await _dbContext.OperationHistories
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
                .FirstOrDefaultAsync(
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


    public async Task<IReadOnlyList<OperationHistory>> GetEventsAsync(
        string operationId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.OperationHistories
            .AsNoTracking()
            .Where(x => x.OperationId == operationId)
            .OrderBy(x => x.EventId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Operation>> GetForSubmissionAsync(
        DateTime now,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Operations
            .AsNoTracking()
            .Where(x =>
                x.Status == OperationStatus.Created
                ||
                (
                    x.Status == OperationStatus.Processing
                    &&
                    x.NextRetryAt != null
                    &&
                    x.NextRetryAt <= now
                ))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Operation>> GetReadyForRetryAsync(
    CancellationToken cancellationToken)
    {
        return await _dbContext.Operations
            .AsNoTracking()
            .Where(x =>
                x.Status == OperationStatus.Processing &&
                x.NextRetryAt != null &&
                x.NextRetryAt <= DateTime.UtcNow)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Operation>> GetProcessingOperationsAsync(
    CancellationToken cancellationToken)
    {
        return await _dbContext.Operations
            .AsNoTracking()
            .Where(x =>
                x.Status == OperationStatus.Processing &&
                x.NextRetryAt == null)
            .ToListAsync(cancellationToken);
    }

}
