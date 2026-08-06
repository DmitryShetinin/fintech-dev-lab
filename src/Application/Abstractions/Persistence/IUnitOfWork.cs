using System.Threading;

namespace Application.Interface
{
  public interface IUnitOfWork
  {
    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken);

    Task BeginTransactionAsync(
        CancellationToken cancellationToken);

     
  }
}
