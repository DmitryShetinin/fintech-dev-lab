namespace Infrastructure.Configuration;

public sealed class DatabaseOptions
{
  public int CommandTimeoutSeconds { get; set; } = 30;


  public bool EnableRetryOnFailure { get; set; } = true;
  public int MaxRetryCount { get; set; } = 3;
}
