using System.Globalization;
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
      OperationId = operation.Id,
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
      OperationId = operation.Id,
      Status = operation.Status,
      ProviderPaymentId = operation.ProviderPaymentId
    };
  }

  public static ProviderRequest ToProviderRequest(
      this Operation operation)
  {
     return new ProviderRequest
    {
        OperationId = operation.Id,
        Amount = operation.Amount.ToString("0.00", CultureInfo.InvariantCulture),
        Currency = operation.Currency
    };
  }

}
