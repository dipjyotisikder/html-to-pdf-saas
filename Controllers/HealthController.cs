using Microsoft.AspNetCore.Mvc;

namespace HTPDF.Controllers;

/// <summary>
/// Health check controller for monitoring service status.
/// </summary>
[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Basic health check endpoint.
    /// </summary>
    /// <returns>Status of the service.</returns>
    [HttpGet]
    public ActionResult GetHealth()
    {
        return Ok(new 
        { 
            status = "Healthy", 
            timestamp = DateTime.UtcNow,
            service = "HTML to PDF API"
        });
    }
}
