using Core.Enums;

namespace Application.Common.Failures;

public sealed record ProviderFailure(
    ProviderFailureReason Reason,
    string Message)
    : IFailure;