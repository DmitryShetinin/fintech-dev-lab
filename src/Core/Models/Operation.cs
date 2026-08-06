using Core.Enums;
namespace Core.Models;


public class Operation
{
    public string Id { get; private set; } = null!;


    public decimal Amount { get; private set; }


    public string Currency { get; private set; } = null!;


    public string Description { get; private set; } = null!;




    public string? ProviderPaymentId { get; private set; }


    public PaymentProvider Provider { get; private set; }


    public int Version { get; private set; }



    private Operation()
    {
    }



    private Operation(
        string operationId,
        decimal amount,
        string currency,
        string description)
    {
        Id = operationId;

        Amount = amount;
        Currency = currency;
        Description = description;

        Status = OperationStatus.Created;
    }



    public static Operation Create(
        string operationId,
        decimal amount,
        string currency,
        string description)
    {
        return new Operation(
            operationId,
            amount,
            currency,
            description);
    }

    public OperationStatus Status { get; private set; }

    public int RetryCount { get; private set; }

    public DateTime? NextRetryAt { get; private set; }


   


    
    public void ScheduleRetry(TimeSpan delay)
    {
        RetryCount++;

        NextRetryAt = DateTime.UtcNow.Add(delay);
    }

    public void ResetRetry()
    {
        RetryCount = 0;
        NextRetryAt = null;
    }

  


   



    public void SetProviderPaymentId(
        string providerPaymentId)
    {
        if (ProviderPaymentId is null)
        {
            ProviderPaymentId = providerPaymentId;

            return;
        }


        if (ProviderPaymentId != providerPaymentId)
        {
            throw new InvalidOperationException(
                "ProviderPaymentId mismatch.");
        }
    }




    internal OperationEvent MoveTo(
        OperationStatus next)
    {
      

        var previous = Status;


        Status = next;


        return OperationEvent.Create(
            Id,
            previous,
            next,
            $"Operation moved {previous} -> {next}");
    }
}