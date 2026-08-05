

namespace Application.Abstractions.Telemetry;

public interface IOperationMetrics
{
  void OperationCompleted();
  void RetryOccurred();
  void OperationRejected();

}
