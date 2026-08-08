namespace Application.Common.Failures;



 
public enum FailureCode
{
    Validation,
    Duplicate,
    NotFound,
    Conflict
}

public sealed record ApplicationFailure( 
    FailureCode Code, 
    string Message)
    : IFailure;