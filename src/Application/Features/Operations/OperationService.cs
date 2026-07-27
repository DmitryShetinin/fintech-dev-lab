
using Application.Common;
using Application.Extensions;
using Application.Interface;
using Application.Operations.Requests;
using Application.Operations.Responses;
using Core.Enums;
using Core.Models;
using Microsoft.EntityFrameworkCore;


namespace Application.Operations;


public class OperationService : IOperationService
{
  private readonly IOperationRepository _repository;
  private readonly IUnitOfWork _unitOfWork;
  private readonly OperationStateMachine _stateMachine;


  public OperationService(
      IOperationRepository repository,
      IUnitOfWork unitOfWork,
      OperationStateMachine stateMachine)
  {
    _repository = repository;
    _unitOfWork = unitOfWork;
    _stateMachine = stateMachine;
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


    try
    {
      await _repository.AddAsync(
          operation,
          cancellationToken);


      await _unitOfWork.SaveChangesAsync(
          cancellationToken);
    }
    catch (DbUpdateException)
    {
      // UNIQUE constraint поймал гонку
      return Result<OperationResponse>.Failure(
          $"Operation {request.OperationId} already exists");
    }


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
          $"Operation {operationId} not found");
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

        ничего не создаём,
        возвращаем текущее состояние.
    */

    if (operation.Status != OperationStatus.Created)
    {
      return Result<SubmitOperationResponse>.Success(
          operation.ToSubmitResponse());
    }



    var operationEvent = operation.MoveTo(
        OperationStatus.Processing,
        _stateMachine);



    await _repository.AddEventAsync(
        operationEvent,
        cancellationToken);



    try
    {
      await _unitOfWork.SaveChangesAsync(
          cancellationToken);
    }
    catch (DbUpdateConcurrencyException)
    {
      var actualOperation =
          await _repository.GetByIdAsync(
              operationId,
              cancellationToken);


      if (actualOperation is null)
      {
        return Result<SubmitOperationResponse>.Failure(
            $"Operation {operationId} not found");
      }


      return Result<SubmitOperationResponse>.Success(
          actualOperation.ToSubmitResponse());
    }



    return Result<SubmitOperationResponse>.Success(
        operation.ToSubmitResponse());
  }
}
