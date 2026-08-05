namespace Application.Common.Failures;

public sealed record ApplicationFailure(
    string Message)
    : IFailure;