using System.Threading;

namespace Application.Interfaces;

public interface IUnitOfWork
{
  Task SaveChangesAsync(
      CancellationToken cancellationToken);

  Task BeginTransactionAsync(
      CancellationToken cancellationToken);

  Task CommitTransactionAsync(
      CancellationToken cancellationToken);

  Task RollbackTransactionAsync(
      CancellationToken cancellationToken);
}
