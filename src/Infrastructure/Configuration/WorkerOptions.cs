namespace Infrastructure.Configuration;

public sealed class WorkerOptions
{
  public int SubmissionWorkers { get; set; } = 4;

  public int ReceiptWorkers { get; set; } = 4;
}
