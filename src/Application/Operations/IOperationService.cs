
using Application.Common;
using Application.Operations.Responses;
using Application.Operations.Requests;


namespace Application.Operations;



public interface IOperationService
{
  Task<Result<OperationResponse>> CreateAsync(
      CreateOperationRequest request,
      CancellationToken cancellationToken);



  Task<Result<OperationResponse>> GetAsync(
      string operationId,
      CancellationToken cancellationToken);



  Task<Result<IReadOnlyList<OperationEventResponse>>> GetEventsAsync(
      string operationId,
      CancellationToken cancellationToken);



  Task<Result<SubmitOperationResponse>> SubmitAsync(
      string operationId,
      CancellationToken cancellationToken);
}
