using Core.Enums;

namespace Core.Models;

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

    public void Complete(Operation operation)
    {
        Validate(
            operation.Status,
            OperationStatus.Completed);

        operation.MoveTo(OperationStatus.Completed);
    }

    public void Reject(Operation operation)
    {
        Validate(
            operation.Status,
            OperationStatus.Rejected);

        operation.MoveTo(OperationStatus.Rejected);
        operation.ResetRetry();
    }

    public void StartProcessing(Operation operation)
    {
        Validate(
            operation.Status,
            OperationStatus.Processing);

        operation.MoveTo(OperationStatus.Processing);
    }

  





}

