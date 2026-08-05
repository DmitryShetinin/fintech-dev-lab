using Application.Common;
using Application.Common.Failures;
using Application.Extensions;
using Application.Interface;
using Application.Abstractions.Queue;
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
    private readonly ISubmissionQueue _submissionQueue;


    public OperationService(
        IOperationRepository repository,
        IUnitOfWork unitOfWork,
        OperationStateMachine stateMachine,
        ISubmissionQueue submissionQueue)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _stateMachine = stateMachine;
        _submissionQueue = submissionQueue;
    }



    public async Task<Result<OperationResponse>> CreateAsync(
        CreateOperationRequest request,
        CancellationToken cancellationToken)
    {
        var exists =
            await _repository.ExistsAsync(
                request.OperationId,
                cancellationToken);


        if (exists)
        {
            return Result<OperationResponse>.Failure(
                new ApplicationFailure(
                    $"Operation {request.OperationId} already exists"));
        }


        var operation =
            Operation.Create(
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
            return Result<OperationResponse>.Failure(
                new ApplicationFailure(
                    $"Operation {request.OperationId} already exists"));
        }


        return Result<OperationResponse>.Success(
            operation.ToResponse());
    }




    public async Task<Result<OperationResponse>> SubmitAsync(
        string operationId,
        CancellationToken cancellationToken)
    {
        var operation =
            await _repository.GetByIdAsync(
                operationId,
                cancellationToken);


        if (operation is null)
        {
            return Result<OperationResponse>.Failure(
                new ApplicationFailure(
                    $"Operation {operationId} not found"));
        }



        // Повторный submit.
        // По контракту возвращаем текущее состояние.
        if (operation.Status != OperationStatus.Created)
        {
            return Result<OperationResponse>.Success(
                operation.ToResponse());
        }



        operation.StartProcessing(
            _stateMachine);



        await _unitOfWork.SaveChangesAsync(
            cancellationToken);



        await _submissionQueue.EnqueueAsync(
            operation,
            cancellationToken);



        return Result<OperationResponse>.Success(
            operation.ToResponse());
    }





    public async Task<Result<OperationResponse>> GetAsync(
        string operationId,
        CancellationToken cancellationToken)
    {
        var operation =
            await _repository.GetByIdAsync(
                operationId,
                cancellationToken);


        if (operation is null)
        {
            return Result<OperationResponse>.Failure(
                new ApplicationFailure(
                    $"Operation {operationId} not found"));
        }


        return Result<OperationResponse>.Success(
            operation.ToResponse());
    }





    public async Task<Result<IReadOnlyList<OperationEventResponse>>> GetEventsAsync(
        string operationId,
        CancellationToken cancellationToken)
    {
        var operation =
            await _repository.GetByIdAsync(
                operationId,
                cancellationToken);


        if (operation is null)
        {
            return Result<IReadOnlyList<OperationEventResponse>>.Failure(
                new ApplicationFailure(
                    $"Operation {operationId} not found"));
        }



        var events =
            await _repository.GetEventsAsync(
                operationId,
                cancellationToken);



        var response =
            events
                .Select(x => x.ToResponse())
                .ToList();



        return Result<IReadOnlyList<OperationEventResponse>>.Success(
            response);
    }
}