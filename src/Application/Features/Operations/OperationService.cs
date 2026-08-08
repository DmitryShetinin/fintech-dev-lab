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
 

    private readonly ISubmissionQueue _submissionQueue;

    private readonly IOperationMetrics _operationMetrics;
    private readonly ILogger _logger;

    public OperationService(
        IOperationRepository repository,
        IUnitOfWork unitOfWork,
        OperationStateMachine stateMachine,
        ISubmissionQueue submissionQueue,
        IOperationMetrics operationMetrics, 
        ILogger<OperationService> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _stateMachine = stateMachine;
        _submissionQueue = submissionQueue;
        _operationMetrics = operationMetrics; 
        _logger = logger;
     
    }



    public async Task<Result<OperationResponse>> CreateAsync(
    CreateOperationRequest request,
    CancellationToken cancellationToken)
    {
Console.WriteLine(
    $"CREATE: OperationId={request.OperationId}, Amount={request.Amount}, Currency={request.Currency}");
            if (request.Amount <= 0)
            {
                    Console.WriteLine(">>> NEGATIVE AMOUNT VALIDATION HIT");
                return Result<OperationResponse>.Failure(
                     new ApplicationFailure("Amount must be greater than zero"));
            }

            if (!string.Equals(
                    request.Currency,
                    "RUB",
                    StringComparison.OrdinalIgnoreCase))
            {
                
                return Result<OperationResponse>.Failure(
                      new ApplicationFailure(
                    "Only RUB currency is supported"));
            }

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
Console.WriteLine(
    $"CREATE: OperationId={request.OperationId}, Amount={request.Amount}, Currency={request.Currency}");
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