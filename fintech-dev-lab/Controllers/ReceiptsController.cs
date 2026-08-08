using Application.Receipts;
using Application.Receipts.Requests;
using Microsoft.AspNetCore.Mvc;

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


  [HttpPost]
public async Task<IActionResult> Receive(
    [FromBody] ReceiptRequest request,
    CancellationToken token)
{
    var result = await _receiptService.ProcessAsync(
        request,
        token);

    if (!result.IsSuccess)
    {
        return Conflict(result.Error);
    }

    return NoContent();
}
}