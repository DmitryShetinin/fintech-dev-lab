using Application.Common;
using Application.Extensions;
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



  public async Task<Result<OperationResponse>> CreateAsync(
     CreateOperationRequest request,
     CancellationToken cancellationToken)
  {
    var exists = await _repository.ExistsAsync(
        request.OperationId,
        cancellationToken);


    if (exists)
    {
      return Result<OperationResponse>.Failure(
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



    return Result<OperationResponse>.Success(
        operation.ToResponse());
  }

  public async Task<Result<OperationResponse>> GetAsync(
      string operationId,
      CancellationToken cancellationToken)
  {
    var operation = await _repository.GetByIdAsync(
        operationId,
        cancellationToken);


    if (operation is null)
    {
      return Result<OperationResponse>.Failure(
          "Operation not found");
    }


    return Result<OperationResponse>.Success(
        operation.ToResponse());
  }



  public async Task<Result<IReadOnlyList<OperationEventResponse>>> GetEventsAsync(
      string operationId,
      CancellationToken cancellationToken)
  {
    var operation = await _repository.GetByIdAsync(
        operationId,
        cancellationToken);


    if (operation is null)
    {
      return Result<IReadOnlyList<OperationEventResponse>>.Failure(
          $"Operation {operationId} not found");
    }


    var events = await _repository.GetEventsAsync(
        operationId,
        cancellationToken);


    var response = events
        .Select(x => x.ToResponse())
        .ToList();


    return Result<IReadOnlyList<OperationEventResponse>>.Success(
        response);
  }




  public async Task<Result<SubmitOperationResponse>> SubmitAsync(
      string operationId,
      CancellationToken cancellationToken)
  {
    var operation = await _repository.GetByIdAsync(
        operationId,
        cancellationToken);


    if (operation is null)
    {
      return Result<SubmitOperationResponse>.Failure(
          $"Operation {operationId} not found");
    }



    /*
        Повторный submit:

        PROCESSING
        COMPLETED
        REJECTED

        ничего не создаем
        просто возвращаем состояние
    */


    if (operation.Status != OperationStatus.Created)
    {
      return Result<SubmitOperationResponse>.Success(
          operation.ToSubmitResponse());
    }



    /*
        Здесь позже будет:

        _submissionWorkflow.SubmitAsync(operation)

        который:

        1. создаст PaymentAttempt
        2. сделает MoveTo(PROCESSING)
        3. создаст OperationEvent
        4. SaveChanges()
        5. отправит провайдеру
    */


    var stateMachine = new OperationStateMachine();


    var operationEvent = operation.MoveTo(
        OperationStatus.Processing,
        stateMachine);



    await _repository.AddEventAsync(
        operationEvent,
        cancellationToken);



    await _unitOfWork.SaveChangesAsync(
        cancellationToken);



    return Result<SubmitOperationResponse>.Success(
        operation.ToSubmitResponse());
  }


  private static OperationResponse MapToResponse(
      Operation operation)
  {
    return operation.ToResponse();
  }
}
