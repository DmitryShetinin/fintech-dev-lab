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

  [HttpPost]
  public async Task<IActionResult> Receive(
      ReceiptRequest request,
      CancellationToken cancellationToken)
  {
    await _receiptService.ProcessAsync(
        request,
        cancellationToken);

    return NoContent();
  }
}
