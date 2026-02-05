using HTPDF.Infrastructure.Common;
using Microsoft.AspNetCore.Mvc;

namespace HTPDF.Infrastructure.Common;

[ApiController]
public abstract class BaseApiController : ControllerBase
{
    protected ActionResult HandleResult(Result result)
    {
        if (result.IsSuccess)
        {
            return Ok(new { Message = result.Message });
        }

        return BadRequest(new { Error = result.Message });
    }

    protected ActionResult HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return BadRequest(new { Error = result.Message });
    }
    
    protected ActionResult HandleResultWithAccepted<T>(Result<T> result, string actionName, object routeValues)
    {
        if (result.IsSuccess)
        {
            return AcceptedAtAction(actionName, routeValues, result.Value);
        }

        return BadRequest(new { Error = result.Message });
    }
}
