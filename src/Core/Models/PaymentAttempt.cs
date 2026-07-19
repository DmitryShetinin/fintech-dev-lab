namespace Core.Models
{
  public class PaymentAttempt
  {
    public Guid Id { get; set; }

    public Guid OperationId { get; set; }

    public int AttemptNumber { get; set; }


    public AttemptStatus Status { get; set; }


    public DateTime StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }


    public string? ProviderPaymentId { get; set; }


    public string? Error { get; set; }
  }
}
