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
using Application.Abstractions.Telemetry;
using Microsoft.Extensions.Logging;
using Application.Abstractions.Persistence;


namespace Application.Operations;


public class OperationService : IOperationService
{
    private readonly IOperationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly OperationStateMachine _stateMachine;
     private readonly IOperationEventRepository _operationEventRepository;

    private readonly ISubmissionQueue _submissionQueue;

    private readonly IOperationMetrics _operationMetrics;
    private readonly ILogger _logger;

    public OperationService(
        IOperationRepository repository,
        IUnitOfWork unitOfWork,
        OperationStateMachine stateMachine,
        ISubmissionQueue submissionQueue,
        IOperationMetrics operationMetrics, 
        ILogger<OperationService> logger,  
        IOperationEventRepository operationEventRepository)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _stateMachine = stateMachine;
        _submissionQueue = submissionQueue;
        _operationMetrics = operationMetrics; 
        _logger = logger;
        _operationEventRepository = operationEventRepository; 
    }



    public async Task<Result<OperationResponse>> CreateAsync(
    CreateOperationRequest request,
    CancellationToken cancellationToken)
    {
        var operation =
            Operation.Create(
                request.OperationId,
                request.Amount,
                request.Currency,
                request.Description, 
                request.Provider);


        _operationMetrics.AddOperationCreated();

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(
                async ct =>
                {
                    await _repository.AddAsync(
                        operation,
                        ct);
                
                },
                cancellationToken);
        }
        catch (DbUpdateException ex)
        {
                _logger.LogError(
                ex,
                "Database error while creating operation {OperationId}",
                request.OperationId);


            return Result<OperationResponse>.Failure(
                new ApplicationFailure(
                    "Database error"));
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


        var response =
        await _unitOfWork.ExecuteInTransactionAsync(
        async ct =>
        {
            _stateMachine.StartProcessing(operation);

            await _submissionQueue.EnqueueAsync(
                operation,
                ct);

            return operation.ToResponse();
        },
        cancellationToken);

        return Result<OperationResponse>.Success(response);
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