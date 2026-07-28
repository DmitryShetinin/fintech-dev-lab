

using Core.Enums;

namespace Application.Telemetry;

public interface IOperationMetrics
{
  void OperationCreated();

  void OperationFinished(OperationStatus status);

  void RetryOccurred();

  void CallbackReceived();

  void UpdateProcessingOperations(int count);
}
