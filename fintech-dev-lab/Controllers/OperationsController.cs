
using Microsoft.AspNetCore.Mvc;


namespace fintech_dev_lab.Controllers;

[ApiController]
[Route("operations")]


public class OperationsController : ControllerBase
{
  private readonly IOperationService _operationService;

  public OperationsController(IOperationService operationService)
  {
    _operationService = operationService;
  }

  [HttpPost]
  public async Task<IActionResult> Create(
      CreateOperationRequest request,
      CancellationToken cancellationToken)
  {
    var operation =
        await _operationService.CreateAsync(
            request,
            cancellationToken);

    return CreatedAtAction(
        nameof(GetById),
        new { id = operation.OperationId },
        operation);
  }

  [HttpPost("{id}/submit")]
  public async Task<IActionResult> Submit(
      string id,
      CancellationToken cancellationToken)
  {
    var result =
        await _operationService.SubmitAsync(
            id,
            cancellationToken);

    return result.IsNewSubmission
        ? Accepted(result)
        : Ok(result);
  }

  [HttpGet("{id}")]
  public async Task<IActionResult> GetById(
      string id,
      CancellationToken cancellationToken)
  {
    var operation =
        await _operationService.GetAsync(
            id,
            cancellationToken);

    return Ok(operation);
  }

  [HttpGet("{id}/events")]
  public async Task<IActionResult> GetEvents(
      string id,
      CancellationToken cancellationToken)
  {
    var events =
        await _operationService.GetEventsAsync(
            id,
            cancellationToken);

    return Ok(events);
  }
}

