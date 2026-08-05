using Core.Enums;

namespace Core.Models;


public class Operation
{
    public string Id { get; private set; } = null!;


    public decimal Amount { get; private set; }


    public string Currency { get; private set; } = null!;


    public string Description { get; private set; } = null!;


    public OperationStatus Status { get; private set; }


    public string? ProviderPaymentId { get; private set; }


    public PaymentProvider Provider { get; private set; }


    public int Version { get; private set; }



    private Operation()
    {
    }



    private Operation(
        string operationId,
        decimal amount,
        string currency,
        string description)
    {
        Id = operationId;

        Amount = amount;
        Currency = currency;
        Description = description;

        Status = OperationStatus.Created;
    }



    public static Operation Create(
        string operationId,
        decimal amount,
        string currency,
        string description)
    {
        return new Operation(
            operationId,
            amount,
            currency,
            description);
    }




    public OperationEvent StartProcessing(
        OperationStateMachine stateMachine)
    {
        return MoveTo(
            OperationStatus.Processing,
            stateMachine);
    }



    public OperationEvent WaitForReceipt(
        OperationStateMachine stateMachine,
        string providerPaymentId)
    {
        SetProviderPaymentId(providerPaymentId);

        return MoveTo(
            OperationStatus.WaitingForReceipt,
            stateMachine);
    }




    public void ApplyReceipt(
        ProviderPaymentStatus status,
        OperationStateMachine stateMachine)
    {
        switch (status)
        {
            case ProviderPaymentStatus.Succeeded:

                Complete(stateMachine);

                break;


            case ProviderPaymentStatus.Failed:

                Reject(stateMachine);

                break;


            case ProviderPaymentStatus.Pending:

                break;
        }
    }




    public OperationEvent Complete(
        OperationStateMachine stateMachine)
    {
        return MoveTo(
            OperationStatus.Completed,
            stateMachine);
    }



    public OperationEvent Reject(
        OperationStateMachine stateMachine)
    {
        return MoveTo(
            OperationStatus.Rejected,
            stateMachine);
    }




    public void SetProviderPaymentId(
        string providerPaymentId)
    {
        if (ProviderPaymentId is null)
        {
            ProviderPaymentId = providerPaymentId;

            return;
        }


        if (ProviderPaymentId != providerPaymentId)
        {
            throw new InvalidOperationException(
                "ProviderPaymentId mismatch.");
        }
    }




    public OperationEvent MoveTo(
        OperationStatus next,
        OperationStateMachine stateMachine)
    {
        stateMachine.Validate(
            Status,
            next);


        var previous = Status;


        Status = next;


        return OperationEvent.Create(
            Id,
            previous,
            next,
            $"Operation moved {previous} -> {next}");
    }
}