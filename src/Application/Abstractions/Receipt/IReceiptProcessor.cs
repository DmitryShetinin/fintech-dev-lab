 
 
using Core.Models;


namespace Application.Abstractions.Receipt;

public interface IReceiptProcessor
{
    Task ProcessAsync(
        Operation operation,
        CancellationToken token);
}