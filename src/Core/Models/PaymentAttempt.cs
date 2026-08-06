using Core.Enums;

namespace Core.Models;


public enum PaymentAttemptType
{
    Submission,

    ReceiptPolling
}



public class PaymentAttempt
{
    public Guid Id { get; private set; }


    public string OperationId { get; private set; } = null!;


    public PaymentAttemptType Type { get; private set; }


    public int AttemptNumber { get; private set; }


    public AttemptStatus Status { get; private set; }


    public DateTime StartedAt { get; private set; }


    public DateTime? FinishedAt { get; private set; }



    public string? ProviderPaymentId { get; private set; }



    public ProviderFailureReason? FailureReason { get; private set; }


    public string? FailureMessage { get; private set; }







    private PaymentAttempt()
    {
    }



    public static PaymentAttempt Start(
        string operationId,
        int attemptNumber,
        PaymentAttemptType type)
    {
        return new PaymentAttempt
        {
            OperationId = operationId,
            AttemptNumber = attemptNumber,
            Type = type,
            Status = AttemptStatus.Processing,
            StartedAt = DateTime.UtcNow
        };
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



    public void MarkProviderAccepted(
        string providerPaymentId)
    {
        SetProviderPaymentId(providerPaymentId);

        Complete();
    }



    public void Complete()
    {
        Status = AttemptStatus.SUCCESS;

        FinishedAt = DateTime.UtcNow;
    }



    public void Fail(
        ProviderFailureReason reason,
        string message)
    {
        FailureReason = reason;

        FailureMessage = message;

        Status = AttemptStatus.FAILED;

        FinishedAt = DateTime.UtcNow;
    }



    
}



public enum AttemptStatus
{
    Processing,

    SUCCESS,

    FAILED
}