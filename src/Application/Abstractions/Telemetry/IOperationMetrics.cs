

namespace Application.Abstractions.Telemetry;

public interface IOperationMetrics
{
  void AddOperationCompleted();
  void AddRetryOccurred();
  void AddOperationRejected();

  void AddOperationCreated();

}
