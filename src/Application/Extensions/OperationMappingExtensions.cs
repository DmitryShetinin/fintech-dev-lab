using Application.Operations.Responses;
using Application.Provider;
using Core.Models;


namespace Application.Extensions;


public static class OperationMappingExtensions
{

  public static OperationResponse ToResponse(
      this Operation operation)
  {
    return new OperationResponse
    {
      OperationId = operation.OperationId,
      Amount = operation.Amount,
      Currency = operation.Currency,
      Description = operation.Description,
      Status = operation.Status,
      ProviderPaymentId = operation.ProviderPaymentId
    };
  }



  public static SubmitOperationResponse ToSubmitResponse(
      this Operation operation)
  {
    return new SubmitOperationResponse
    {
      OperationId = operation.OperationId,
      Status = operation.Status,
      ProviderPaymentId = operation.ProviderPaymentId
    };
  }

  public static ProviderPaymentRequest ToProviderPaymentRequest(
      this Operation operation)
  {
    return new ProviderPaymentRequest
    {
      OperationId = operation.OperationId,
      Amount = operation.Amount,
      Currency = operation.Currency,
      Description = operation.Description
    };
  }

}
