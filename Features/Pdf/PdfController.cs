using HTPDF.Features.Pdf.Download;
using HTPDF.Features.Pdf.GenerateAsync;
using HTPDF.Features.Pdf.GetStatus;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HTPDF.Features.Pdf;

[ApiController]
[Route("pdf")]
[Authorize]
public class PdfController : ControllerBase
{
    private readonly IMediator _mediator;

    public PdfController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("generate/async")]
    public async Task<ActionResult<GenerateAsyncResult>> GenerateAsync([FromBody] GenerateAsyncRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userEmail = User.FindFirstValue(ClaimTypes.Email)!;

        var command = new GenerateAsyncCommand(
            userId,
            userEmail,
            request.HtmlContent,
            request.Filename,
            request.Orientation ?? "Portrait",
            request.PaperSize ?? "A4"
        );

        var result = await _mediator.Send(command);

        return AcceptedAtAction(nameof(GetStatus), new { jobId = result.JobId }, result);
    }

    [HttpGet("jobs/{jobId}")]
    public async Task<ActionResult<GetStatusResult>> GetStatus(string jobId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var query = new GetStatusQuery(jobId, userId);
        var result = await _mediator.Send(query);

        if (result == null)
        {
            return NotFound(new { Error = "Job Not Found" });
        }

        return Ok(result);
    }

    [HttpGet("jobs/{jobId}/download")]
    public async Task<ActionResult> Download(string jobId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var query = new DownloadQuery(jobId, userId);
        var result = await _mediator.Send(query);

        if (result == null)
        {
            return NotFound(new { Error = "Job Not Found Or Not Yet Completed" });
        }

        return File(result.PdfBytes, "application/pdf", result.Filename);
    }
}

public record GenerateAsyncRequest(
    string HtmlContent,
    string? Filename,
    string? Orientation,
    string? PaperSize
);
