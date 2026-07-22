using Application.Interface;
using Application.Interfaces;
using Application.Operations.Requests;
using Application.Operations.Responses;
using Core.Enums;
using Core.Models;


namespace Application.Operations;


public class OperationService : IOperationService
{
  private readonly IOperationRepository _repository;
  private readonly IUnitOfWork _unitOfWork;


  public OperationService(
      IOperationRepository repository,
      IUnitOfWork unitOfWork)
  {
    _repository = repository;
    _unitOfWork = unitOfWork;
  }



  public async Task<OperationResponse> CreateAsync(
      CreateOperationRequest request,
      CancellationToken cancellationToken)
  {
    var exists = await _repository.ExistsAsync(
        request.OperationId,
        cancellationToken);


    if (exists)
    {
      throw new InvalidOperationException(
          $"Operation {request.OperationId} already exists");
    }


    var operation = Operation.Create(
        request.OperationId,
        request.Amount,
        request.Currency,
        request.Description);


    await _repository.AddAsync(
        operation,
        cancellationToken);


    await _unitOfWork.SaveChangesAsync(
        cancellationToken);


    return MapToResponse(operation);
  }



  public async Task<OperationResponse> GetAsync(
      string operationId,
      CancellationToken cancellationToken)
  {
    var operation = await _repository.GetByIdAsync(
        operationId,
        cancellationToken);


    if (operation is null)
    {
      throw new KeyNotFoundException(
          $"Operation {operationId} not found");
    }


    return MapToResponse(operation);
  }




  public async Task<IReadOnlyList<OperationEventResponse>> GetEventsAsync(
      string operationId,
      CancellationToken cancellationToken)
  {
    var operation = await _repository.GetByIdAsync(
        operationId,
        cancellationToken);


    if (operation is null)
    {
      throw new KeyNotFoundException(
          $"Operation {operationId} not found");
    }


    var events = await _repository.GetEventsAsync(
        operationId,
        cancellationToken);



    return events
        .Select(x => new OperationEventResponse
        {
          EventId = x.EventId,
          Type = x.ToStatus,
          FromStatus = x.FromStatus,
          ToStatus = x.ToStatus,
          Message = x.Message,
          OccurredAt = x.OccurredAt
        })
        .ToList();
  }




  public async Task<SubmitOperationResponse> SubmitAsync(
      string operationId,
      CancellationToken cancellationToken)
  {
    var operation = await _repository.GetByIdAsync(
        operationId,
        cancellationToken);


    if (operation is null)
    {
      throw new KeyNotFoundException(
          $"Operation {operationId} not found");
    }



    // Повторный submit
    // CREATED -> надо запускать workflow
    // PROCESSING/COMPLETED/REJECTED -> просто вернуть состояние

    if (operation.Status != OperationStatus.Created)
    {
      return new SubmitOperationResponse
      {
        OperationId = operation.OperationId,
        Status = operation.Status,
        ProviderPaymentId = operation.ProviderPaymentId
      };
    }



    /*
        Здесь позже будет:

        await _submissionWorkflow.SubmitAsync(
            operation,
            cancellationToken);


        Workflow сделает:

        1. Создать PaymentAttempt
        2. Operation.MoveTo(PROCESSING)
        3. Создать OperationEvent
        4. SaveChanges()
        5. Отправить провайдеру

    */



    return new SubmitOperationResponse
    {
      OperationId = operation.OperationId,
      Status = operation.Status,
      ProviderPaymentId = operation.ProviderPaymentId
    };
  }




  private static OperationResponse MapToResponse(
      Operation operation)
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
}
