using Application.Operations.Responses;



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


    return new OperationResponse
    {
      OperationId = operation.Id,
      Status = operation.Status
    };
  }
}



