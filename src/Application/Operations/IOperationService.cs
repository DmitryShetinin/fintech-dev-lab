
using Application.Operations.Responses;
using Application.Operations.Requests;


namespace Application.Operations;


public interface IOperationService
{
    Task<OperationResponse> CreateAsync(
        CreateOperationRequest request,
        CancellationToken cancellationToken);

    Task<SubmitOperationResponse> SubmitAsync(
        string operationId,
        CancellationToken cancellationToken);

    Task<OperationResponse> GetAsync(
        string operationId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationEventResponse>> GetEventsAsync(
        string operationId,
        CancellationToken cancellationToken);
}
