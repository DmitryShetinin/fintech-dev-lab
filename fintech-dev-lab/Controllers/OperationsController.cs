
using Application.Operations;
using Application.Operations.Requests;
using Microsoft.AspNetCore.Mvc;


namespace fintech_dev_lab.Controllers;


[ApiController]
[Route("operations")]
public class OperationsController : ControllerBase
{
  private readonly IOperationService _operationService;


  public OperationsController(
      IOperationService operationService)
  {
    _operationService = operationService;
  }


  [HttpPost]
  public async Task<IActionResult> Create(
      [FromBody] CreateOperationRequest request,
      CancellationToken cancellationToken)
  {
    var result = await _operationService.CreateAsync(
        request,
        cancellationToken);


    if (!result.IsSuccess)
    {
      return BadRequest(result.Error);
    }


    return CreatedAtAction(
        nameof(GetById),
        new
        {
          id = result.Value!.OperationId
        },
        result.Value);
  }


  [HttpPost("{id}/submit")]
  public async Task<IActionResult> Submit(
      string id,
      CancellationToken cancellationToken)
  {
    var result = await _operationService.SubmitAsync(
        id,
        cancellationToken);


    if (!result.IsSuccess)
    {
      return BadRequest(result.Error);
    }


    return Accepted(result.Value);
  }


  [HttpGet("{id}")]
  public async Task<IActionResult> GetById(
      string id,
      CancellationToken cancellationToken)
  {
    var result = await _operationService.GetAsync(
        id,
        cancellationToken);


    if (!result.IsSuccess)
    {
      return NotFound(result.Error);
    }


    return Ok(result.Value);
  }


  [HttpGet("{id}/events")]
  public async Task<IActionResult> GetEvents(
      string id,
      CancellationToken cancellationToken)
  {
    var result = await _operationService.GetEventsAsync(
        id,
        cancellationToken);


    if (!result.IsSuccess)
    {
      return NotFound(result.Error);
    }


    return Ok(result.Value);
  }
}
