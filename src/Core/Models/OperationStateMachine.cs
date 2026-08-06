using Core.Enums;

namespace Core.Models
{
  public class OperationStateMachine
  {
    private static readonly Dictionary<
        OperationStatus,
        HashSet<OperationStatus>>
        Transitions =
    new()
    {
        {
            OperationStatus.Created,
            [
                OperationStatus.Processing
            ]
        },

        {
            OperationStatus.Processing,
            [
                OperationStatus.Completed,
                OperationStatus.Rejected
            ]
        },

        {
            OperationStatus.Completed,
            []
        },

        {
            OperationStatus.Rejected,
            []
        }
    };


    public void Validate(
        OperationStatus from,
        OperationStatus to)
    {
      if (!Transitions[from]
          .Contains(to))
      {
        throw new InvalidOperationException(
            $"Transition {from} -> {to} is forbidden");
      }
    }

    public OperationEvent Complete(Operation operation)
    {
        return operation.MoveTo(OperationStatus.Completed);
    }

    public OperationEvent Reject(Operation operation)
    {
        return operation.MoveTo(OperationStatus.Rejected);
    }

    public OperationEvent StartProcessing(Operation operation)
    {
        return operation.MoveTo(OperationStatus.Rejected);
    
    }

    public OperationEvent WaitForReceipt(Operation operation,string providerPaymentId)
    {
          
        operation.SetProviderPaymentId(providerPaymentId); 
        return operation.MoveTo(OperationStatus.WaitingForReceipt);
    }


  



  }
}
