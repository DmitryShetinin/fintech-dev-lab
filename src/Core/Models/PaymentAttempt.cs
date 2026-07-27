using Core.Enums;

namespace Core.Models;

public class PaymentAttempt
{
  public Guid Id { get; set; }

  public string OperationId { get; set; }


  public int AttemptNumber { get; set; }


  public AttemptStatus Status { get; set; }


  public DateTime StartedAt { get; set; }

  public DateTime? FinishedAt { get; set; }


  public string? ProviderPaymentId { get; set; }


  public ProviderFailureReason? FailureReason { get; private set; }


  public string? FailureMessage { get; private set; }

  public static PaymentAttempt Start(
      string operationId,
      int attemptNumber)
  {
    return new PaymentAttempt
    {
      OperationId = operationId,
      AttemptNumber = attemptNumber,
      Status = AttemptStatus.ProviderAccepted,
      StartedAt = DateTime.UtcNow
    };
  }
  public void MarkProviderAccepted(string ProviderPaymentId)
  {
    this.ProviderPaymentId = ProviderPaymentId;
    Status = AttemptStatus.SUCCESS;
    FinishedAt = DateTime.UtcNow;

  }

  public void Fail(ProviderFailureReason reason, string Error)
  {

    FailureReason = reason;
    FailureMessage = Error;
    Status = AttemptStatus.FAILED;
    FinishedAt = DateTime.UtcNow;

  }

}

public enum AttemptStatus
{
  SUCCESS,

  ProviderAccepted,

  FAILED
}
