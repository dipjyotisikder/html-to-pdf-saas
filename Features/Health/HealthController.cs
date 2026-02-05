using Microsoft.AspNetCore.Mvc;

namespace HTPDF.Features.Health;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult GetHealth()
    {
        return Ok(new 
        { 
            Status = "Healthy", 
            Timestamp = DateTime.UtcNow,
            Service = "HTML To PDF API",
            Version = "3.0",
            Architecture = "Vertical Slice"
        });
    }
}
