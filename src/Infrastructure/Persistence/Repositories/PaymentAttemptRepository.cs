using Application.Abstractions.Persistence;
 
using Core.Models;
 
using Microsoft.EntityFrameworkCore;


namespace Infrastructure.Persistence.Repositories;


public class PaymentAttemptRepository 
    : IPaymentAttemptRepository
{

    private readonly AppDbContext _context;


    public PaymentAttemptRepository(
        AppDbContext context)
    {
        _context = context;
    }



    public async Task AddAsync(
        PaymentAttempt attempt,
        CancellationToken cancellationToken)
    {
        await _context.PaymentAttempts
            .AddAsync(
                attempt,
                cancellationToken);
    }





    public async Task<IReadOnlyList<PaymentAttempt>> GetByOperationIdAsync(
        string operationId,
        CancellationToken cancellationToken)
    {
        return await _context.PaymentAttempts
            .Where(x => x.OperationId == operationId)
            .OrderBy(x => x.AttemptNumber)
            .ToListAsync(cancellationToken);
    }





    public async Task<int> GetNextAttemptNumberAsync(
        string operationId,
        PaymentAttemptType type,
        CancellationToken cancellationToken)
    {
        var lastAttempt =
            await _context.PaymentAttempts
                .Where(x =>
                    x.OperationId == operationId &&
                    x.Type == type)
                .MaxAsync(
                    x => (int?)x.AttemptNumber,
                    cancellationToken);


        return (lastAttempt ?? 0) + 1;
    }

    public async Task<IReadOnlyList<PaymentAttempt>> GetReadyForRetryAsync(
    PaymentAttemptType type,
    DateTime now,
    CancellationToken cancellationToken)
{
    return await _context.PaymentAttempts
        .Where(x =>
            x.Type == type &&
            x.Status == AttemptStatus.FAILED &&
            x.NextRetryAt <= now)
        .ToListAsync(cancellationToken);
}
}