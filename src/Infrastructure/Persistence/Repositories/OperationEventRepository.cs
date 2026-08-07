using Application.Abstractions.Persistence;
using Core.Models;

namespace Infrastructure.Persistence;

public sealed class OperationEventRepository 
    : IOperationEventRepository
{
    private readonly AppDbContext _dbContext;


    public OperationEventRepository(
        AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }


    public async Task AddAsync(
        OperationHistory history,
        CancellationToken token)
    {
        await _dbContext.OperationHistories
            .AddAsync(
                history,
                token);
    }
}