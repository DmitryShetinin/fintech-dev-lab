using Microsoft.AspNetCore.Mvc;


namespace fintech_dev_lab.Controllers;


[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
  [HttpGet]
  public IActionResult Get()
  {
    return Ok();
  }
}
