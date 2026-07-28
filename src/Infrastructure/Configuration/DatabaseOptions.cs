namespace Infrastructure.Configuration;

public sealed class DatabaseOptions
{
  public int CommandTimeoutSeconds { get; set; } = 30;
}
