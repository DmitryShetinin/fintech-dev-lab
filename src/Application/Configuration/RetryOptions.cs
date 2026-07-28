namespace Application.Configuration;

public sealed class RetryOptions
{
  public int MaxAttempts { get; set; } = 5;

  public int InitialDelaySeconds { get; set; } = 5;

  public int MaxDelaySeconds { get; set; } = 300;
}
