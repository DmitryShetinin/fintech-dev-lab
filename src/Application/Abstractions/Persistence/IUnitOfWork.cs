using System.Threading;

namespace Application.Interface
{
  public interface IUnitOfWork
  {
    Task<T> ExecuteInTransactionAsync<T>(
      Func<CancellationToken, Task<T>> action,
      CancellationToken cancellationToken);

    Task BeginTransactionAsync(
        CancellationToken cancellationToken);


    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken);


  }
}
