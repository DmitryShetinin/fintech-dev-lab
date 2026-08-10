using Core.DomainEvents;
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
        string description,
        PaymentProvider provider)
    {
        Id = operationId;

        Amount = amount;
        Currency = currency;
        Description = description;
        ProviderPaymentId = null;
        Provider = provider;
        Status = OperationStatus.Created;


        Raise(
            new OperationStateChangedEvent(
                Id,
                null,
                OperationStatus.Created,
                "operation  created",
                DateTime.UtcNow));

    }



    public static Operation Create(
        string operationId,
        decimal amount,
        string currency,
        string description,
        PaymentProvider provider)
    {
        return new Operation(
            operationId,
            amount,
            currency,
            description,
            provider);
    }

    public OperationStatus Status { get; private set; }

    public int RetryCount { get; private set; }

    public DateTime? NextRetryAt { get; private set; }






    public void ScheduleRetry(TimeSpan delay)
    {
        RetryCount++;

        NextRetryAt = DateTime.UtcNow.Add(delay);

            Console.WriteLine(
        $"RETRY SCHEDULED: RetryCount={RetryCount}, NextRetryAt={NextRetryAt:O}");

    }

    public void ResetRetry()
    {
  
        NextRetryAt = null;
    }








    public void SetProviderPaymentId(string providerPaymentId)
    {
        if (string.IsNullOrWhiteSpace(providerPaymentId))
            throw new ArgumentException(
                "Provider payment ID cannot be empty.",
                nameof(providerPaymentId));

        if (ProviderPaymentId is null)
        {
            ProviderPaymentId = providerPaymentId;
            return;
        }

        if (ProviderPaymentId != providerPaymentId)
            throw new InvalidOperationException(
                "ProviderPaymentId mismatch.");
    }


    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents
        => _domainEvents.AsReadOnly();

    private void Raise(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    internal void MoveTo(
        OperationStatus next)
    {


        var previous = Status;


        Status = next;
        var domainEvent =
            new OperationStateChangedEvent(
                Id,
                previous,
                next,
                $"Operation moved {previous} -> {next}",
                DateTime.UtcNow);

        Raise(domainEvent);


    }
}