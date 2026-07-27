using Application.Abstractions.Persistence;
using Application.Abstractions.Providers;
using Application.Common;
using Application.Extensions;
using Application.Interface;
using Application.Interfaces;
using Application.Operations.Responses;
using Core.Enums;
using Core.Models;

namespace Application.Features.Workflows;



public class SubmissionWorkflow
{
  private readonly IOperationRepository _repository;
  private readonly IUnitOfWork _unitOfWork;
  private readonly IProviderClient _providerClient;
  private readonly OperationStateMachine _stateMachine;

  public SubmissionWorkflow(OperationStateMachine stateMachine)
  {
    _stateMachine = stateMachine;
  }


  public async Task<Result<SubmitOperationResponse>> ExecuteAsync(
      string operationId,
      CancellationToken cancellationToken)
  {
    var operation =
        await _repository.GetByIdAsync(
            operationId,
            cancellationToken);


    if (operation is null)
      return Result<SubmitOperationResponse>.Failure(
          "Operation not found");


    if (operation.Status != OperationStatus.Created)
    {
      return Result<SubmitOperationResponse>.Success(
          operation.ToSubmitResponse());
    }


    var operationEvent =
    operation.StartProcessing(_stateMachine);

    await _repository.AddEventAsync(
        operationEvent,
        cancellationToken);

    await _unitOfWork.SaveChangesAsync(
        cancellationToken);

    return Result<SubmitOperationResponse>.Success(
        new SubmitOperationResponse
        {
          OperationId = operation.OperationId,
          Status = operation.Status,
          ProviderPaymentId = operation.ProviderPaymentId
        });

  }
}

