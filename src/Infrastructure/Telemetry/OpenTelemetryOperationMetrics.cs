using System.Diagnostics.Metrics;
using Application.Abstractions.Telemetry;

namespace Infrastructure.Telemetry;

public sealed class OpenTelemetryOperationMetrics
    : IOperationMetrics
{
  private static readonly Meter Meter =
      new("FintechDevLab");

  private readonly Counter<long> _completedCounter;
  private readonly Counter<long> _rejectedCounter;
  private readonly Counter<long> _retryCounter;


  public OpenTelemetryOperationMetrics()
  {
    _completedCounter =
        Meter.CreateCounter<long>(
            "operations.completed");

    _rejectedCounter =
        Meter.CreateCounter<long>(
            "operations.rejected");

    _retryCounter =
        Meter.CreateCounter<long>(
            "operations.retry");
  }


  public void OperationCompleted()
  {
    _completedCounter.Add(1);
  }


  public void OperationRejected()
  {
    _rejectedCounter.Add(1);
  }


  public void RetryOccurred()
  {
    _retryCounter.Add(1);
  }
}
