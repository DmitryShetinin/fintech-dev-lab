using Core.Enums;

namespace Core.Models;


public class Operation
{

  public string OperationId { get; private set; }

  public decimal Amount { get; private set; }

  public string Currency { get; private set; }

  public string Description { get; private set; }

  public OperationStatus Status { get; private set; }

  public string? ProviderPaymentId { get; private set; }


  private readonly List<OperationEvent> _events = [];
  public IReadOnlyCollection<OperationEvent> Events => _events;



  private Operation()
  {
  }

  private Operation(
      string operationId,
      decimal amount,
      string currency,
      string description)
  {

    OperationId = operationId;
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

  public OperationEvent StartProcessing(
      OperationStateMachine stateMachine)
  {
    return MoveTo(
        OperationStatus.Processing,
        stateMachine);
  }


  public DateTime? LastAttemptAt { get; private set; }

  public int RetryCount { get; private set; }

  public DateTime? NextRetryAt { get; private set; }

  public void ScheduleNextRetry(DateTime now, TimeSpan delay)
  {
    RetryCount++;
    LastAttemptAt = now;
    NextRetryAt = now.Add(delay);
  }


  public void AttachProviderPayment(string providerPaymentId)
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
  public OperationEvent MoveTo(
  OperationStatus next,
  OperationStateMachine stateMachine)
  {
    stateMachine.Validate(Status, next);

    var previous = Status;

    Status = next;

    return OperationEvent.Create(
        OperationId,
        previous,
        next,
        $"Operation moved {previous} -> {next}");
  }



}

