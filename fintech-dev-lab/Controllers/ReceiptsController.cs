using Application.Receipts;
using Microsoft.AspNetCore.Mvc;


namespace fintech_dev_lab.Controllers;


[ApiController]
[Route("receipts")]
public class ReceiptsController : ControllerBase
{
  private readonly IReceiptService _receiptService;


  public ReceiptsController(
      IReceiptService receiptService)
  {
    _receiptService = receiptService;
  }


  [HttpGet("{operationId}")]
  public async Task<IActionResult> Get(
      string operationId,
      CancellationToken cancellationToken)
  {
    var result = await _receiptService.GetAsync(
        operationId,
        cancellationToken);


    if (!result.IsSuccess)
    {
      return NotFound(result.Error);
    }


    return Ok(result.Value);
  }
}
