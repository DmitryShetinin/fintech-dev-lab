using Application.Interface;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
  private readonly AppDbContext _dbContext;

  private IDbContextTransaction? _transaction;


  public UnitOfWork(
      AppDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public async Task ExecuteInTransactionAsync(
    Func<CancellationToken, Task> action,
    CancellationToken cancellationToken)
  {
    await BeginTransactionAsync(cancellationToken);

    try
    {
      await action(cancellationToken);

      await SaveChangesAsync(cancellationToken);

      await CommitTransactionAsync(cancellationToken);
    }
    catch
    {
      await RollbackTransactionAsync(cancellationToken);
      throw;
    }
  }
  
  public async Task BeginTransactionAsync(
      CancellationToken cancellationToken)
  {
    _transaction =
        await _dbContext.Database
            .BeginTransactionAsync(cancellationToken);
  }


  public async Task SaveChangesAsync(
      CancellationToken cancellationToken)
  {
    await _dbContext.SaveChangesAsync(cancellationToken);
  }


  public async Task CommitTransactionAsync(
      CancellationToken cancellationToken)
  {
    if (_transaction is null)
      return;


    await _transaction.CommitAsync(cancellationToken);

    await _transaction.DisposeAsync();

    _transaction = null;
  }


  public async Task RollbackTransactionAsync(
      CancellationToken cancellationToken)
  {
    if (_transaction is null)
      return;


    await _transaction.RollbackAsync(cancellationToken);

    await _transaction.DisposeAsync();

    _transaction = null;
  }
}
