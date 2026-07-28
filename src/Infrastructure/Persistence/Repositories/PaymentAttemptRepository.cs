using Application.Abstractions.Persistence;
using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class PaymentAttemptRepository : IPaymentAttemptRepository
{


  private readonly AppDbContext _dbContext;



  public PaymentAttemptRepository(
      AppDbContext context)
  {
    _dbContext = context;
  }


  public async Task AddAsync(
      PaymentAttempt attempt,
      CancellationToken cancellationToken)
  {
    await _dbContext.PaymentAttempts.AddAsync(
        attempt,
        cancellationToken);
  }


  public async Task<IReadOnlyList<PaymentAttempt>> GetByOperationIdAsync(
      string operationId,
      CancellationToken cancellationToken)
  {
    return await _dbContext.PaymentAttempts
        .Where(x => x.OperationId == operationId)
        .OrderBy(x => x.AttemptNumber)
        .ToListAsync(cancellationToken);
  }


  public async Task<PaymentAttempt?> GetByProviderPaymentIdAsync(
      string providerPaymentId,
      CancellationToken cancellationToken)
  {
    return await _dbContext.PaymentAttempts
        .FirstOrDefaultAsync(
            x => x.ProviderPaymentId == providerPaymentId,
            cancellationToken);
  }
}
