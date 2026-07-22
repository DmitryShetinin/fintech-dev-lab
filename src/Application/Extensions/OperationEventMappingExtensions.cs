using Application.Operations.Responses;
using Core.Models;


namespace Application.Extensions;


public static class OperationEventMappingExtensions
{

  public static OperationEventResponse ToResponse(
      this OperationEvent operationEvent)
  {
    return new OperationEventResponse
    {
      EventId = operationEvent.EventId,
      Type = operationEvent.Type,
      FromStatus = operationEvent.FromStatus,
      ToStatus = operationEvent.ToStatus,
      Message = operationEvent.Message,
      OccurredAt = operationEvent.OccurredAt
    };
  }

}
