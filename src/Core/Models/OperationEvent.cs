using Core.Enums;

namespace Core.Models
{
  public class OperationHistory
  {
    public long EventId { get; private set; }

    public string OperationId { get; private set; }

    public OperationStatus? FromStatus { get; private set; }

    public OperationStatus ToStatus { get; private set; }
    public string Type { get; private set; }

    public string Message { get; private set; }

    public DateTime OccurredAt { get; private set; }


    private OperationHistory()
    {
    }


    private OperationHistory(
        string operationId,
        OperationStatus? fromStatus,
        OperationStatus toStatus,
        string type,
        string message)
    {
      OperationId = operationId;
      FromStatus = fromStatus;
      ToStatus = toStatus;
      Type = type;
      Message = message;
      OccurredAt = DateTime.UtcNow;
    }


    public static OperationHistory Create(
        string operationId,
        OperationStatus? fromStatus,
        OperationStatus toStatus,
        string message)
    {
      return new OperationHistory(
          operationId,
          fromStatus,
          toStatus,
          toStatus.ToString(),
          message);
    }
  }
}
