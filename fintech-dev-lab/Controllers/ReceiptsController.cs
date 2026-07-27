using Application.Abstractions.Receipt;
using Application.Receipts;
using Application.Receipts.Requests;
using Microsoft.AspNetCore.Mvc;


namespace fintech_dev_lab.Controllers;


[ApiController]
[Route("receipts")]
public class ReceiptsController : ControllerBase
{
  private readonly IReceiptService _receiptService;

  public ReceiptsController(IReceiptService receiptService)
  {
    _receiptService = receiptService;
  }


  public IReceiptService ReceiptService { get; }

  [HttpPost("receipts")]
  public async Task<IActionResult> Receive(
      ReceiptRequest request,
      CancellationToken token)
  {
    var result =
        await _receiptService.ProcessAsync(
            request,
            token);


    if (!result.IsSuccess)
    {
      return Conflict(result.Error);
    }


    return NoContent();
  }
}
