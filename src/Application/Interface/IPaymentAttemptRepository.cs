using Core.Models;

namespace Application.Abstractions.Persistence;

 
 
 


public interface IPaymentAttemptRepository
{
    Task AddAsync(
        PaymentAttempt attempt,
        CancellationToken cancellationToken);



    Task<IReadOnlyList<PaymentAttempt>> GetByOperationIdAsync(
        string operationId,
        CancellationToken cancellationToken);



    Task<int> GetNextAttemptNumberAsync(
        string operationId,
        PaymentAttemptType type,
        CancellationToken cancellationToken);

      
}