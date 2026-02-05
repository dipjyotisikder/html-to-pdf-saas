using HTPDF.Features.Pdf.DeleteJob;
using HTPDF.Features.Pdf.Download;
using HTPDF.Features.Pdf.GenerateAsync;
using HTPDF.Features.Pdf.GetDashboard;
using HTPDF.Features.Pdf.GetStatus;
using HTPDF.Features.Pdf.GetUserJobs;
using HTPDF.Infrastructure.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HTPDF.Features.Pdf;

[Route("pdf")]
[Authorize]
public class PdfController : BaseApiController
{
    private readonly IMediator _mediator;

    public PdfController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult> GetDashboard()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var query = new GetDashboardQuery(userId);
        var result = await _mediator.Send(query);

        return HandleResult(result);
    }

    [HttpPost("generate/async")]
    public async Task<ActionResult> GenerateAsync([FromBody] GenerateAsyncRequest request)
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

        return HandleResultWithAccepted(result, nameof(GetStatus), new { jobId = result.Value?.JobId });
    }

    [HttpGet("jobs")]
    public async Task<ActionResult> GetUserJobs(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var query = new GetUserJobsQuery(userId, pageNumber, pageSize, status);
        var result = await _mediator.Send(query);

        return HandleResult(result);
    }

    [HttpGet("jobs/{jobId}")]
    public async Task<ActionResult> GetStatus(string jobId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var query = new GetStatusQuery(jobId, userId);
        var result = await _mediator.Send(query);

        return HandleResult(result);
    }

    [HttpGet("jobs/{jobId}/download")]
    public async Task<ActionResult> Download(string jobId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var query = new DownloadQuery(jobId, userId);
        var result = await _mediator.Send(query);

        if (result.IsFailure)
        {
            return BadRequest(new { Error = result.Message });
        }

        return File(result.Value!.PdfBytes, Constants.PdfMimeType, result.Value.Filename);
    }

    [HttpDelete("jobs/{jobId}")]
    public async Task<ActionResult> DeleteJob(string jobId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var command = new DeleteJobCommand(jobId, userId);
        var result = await _mediator.Send(command);

        return HandleResult(result);
    }
}


public record GenerateAsyncRequest(
    string HtmlContent,
    string? Filename,
    string? Orientation,
    string? PaperSize
);
