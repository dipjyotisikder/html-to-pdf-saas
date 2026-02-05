using Microsoft.AspNetCore.Mvc;
using HTPDF.Models;
using HTPDF.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace HTPDF.Controllers;

/// <summary>
/// Controller for handling PDF related requests.
/// </summary>
[ApiController]
[Route("pdf")]
[Authorize] // Require authentication for all endpoints
public class PDFController(
    ILogger<PDFController> logger, 
    IPdfMaker pdfMaker,
    IHtmlSanitizerService htmlSanitizer,
    IPdfJobService jobService) : ControllerBase
{
    private readonly ILogger<PDFController> _logger = logger;
    private readonly IPdfMaker _pdfMaker = pdfMaker;
    private readonly IHtmlSanitizerService _htmlSanitizer = htmlSanitizer;
    private readonly IPdfJobService _jobService = jobService;

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private string GetUserEmail() => User.FindFirstValue(ClaimTypes.Email)!;

    /// <summary>
    /// Endpoint to get a PDF document from hardcoded data (demo/testing).
    /// </summary>
    /// <returns>PDF file as a byte array.</returns>
    [HttpGet("demo")]
    [AllowAnonymous] // Allow demo endpoint without auth
    public ActionResult GetDemo()
    {
        _logger.LogInformation("Demo PDF generation requested");
        var pdfBytes = _pdfMaker.CreatePDF();

        return File(pdfBytes, Constants.PDF, "Demo.pdf");
    }

    /// <summary>
    /// Endpoint to get a chunked PDF document from hardcoded data (demo/testing).
    /// </summary>
    /// <returns>Chunked PDF file as a byte array.</returns>
    [HttpGet("demo/chunked")]
    [AllowAnonymous] // Allow demo endpoint without auth
    public ActionResult GetDemoChunked()
    {
        _logger.LogInformation("Demo chunked PDF generation requested");
        var pdfBytes = _pdfMaker.CreateChunkedPDF();

        return File(pdfBytes, Constants.PDF, "DemoChunked.pdf");
    }

    /// <summary>
    /// Generates a PDF synchronously from custom HTML content.
    /// Use this for small, quick PDFs. For larger documents, use the async endpoint.
    /// </summary>
    /// <param name="request">PDF generation request containing HTML content.</param>
    /// <returns>PDF file as a byte array.</returns>
    /// <response code="200">Returns the generated PDF file.</response>
    /// <response code="400">If the request is invalid or HTML content is too large.</response>
    /// <response code="401">If user is not authenticated.</response>
    /// <response code="429">If rate limit is exceeded.</response>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public ActionResult GeneratePdf([FromBody] PdfGenerationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = GetUserId();
            _logger.LogInformation("Synchronous PDF generation requested by user {UserId}", userId);

            // Sanitize HTML to prevent XSS
            var sanitizedHtml = _htmlSanitizer.Sanitize(request.HtmlContent);

            // Generate PDF
            var pdfBytes = _pdfMaker.CreatePDF(sanitizedHtml, request.Orientation, request.PaperSize);

            var filename = string.IsNullOrWhiteSpace(request.Filename) ? "document.pdf" : request.Filename;
            
            if (!filename.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                filename += ".pdf";
            }

            return File(pdfBytes, Constants.PDF, filename);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for PDF generation");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF");
            return StatusCode(500, new { error = "An error occurred while generating the PDF" });
        }
    }

    /// <summary>
    /// Submits a PDF generation job for asynchronous processing.
    /// Use this for large PDFs or when you don't need immediate results.
    /// The PDF will be emailed to you when ready.
    /// </summary>
    /// <param name="request">PDF generation request containing HTML content.</param>
    /// <returns>Job information with job ID for tracking.</returns>
    /// <response code="202">Returns the job ID and tracking information.</response>
    /// <response code="400">If the request is invalid.</response>
    /// <response code="401">If user is not authenticated.</response>
    /// <response code="429">If rate limit is exceeded.</response>
    [HttpPost("generate/async")]
    [ProducesResponseType(typeof(PdfGenerationResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<PdfGenerationResponse>> GeneratePdfAsync([FromBody] PdfGenerationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = GetUserId();
            var userEmail = GetUserEmail();

            _logger.LogInformation("Async PDF generation requested by user {UserId}", userId);

            // Submit job
            var jobId = await _jobService.SubmitJobAsync(request, userId, userEmail);

            var response = new PdfGenerationResponse
            {
                JobId = jobId,
                Status = "Pending",
                Message = "PDF generation job has been queued. You will receive an email when it's ready.",
                CreatedAt = DateTime.UtcNow
            };

            return AcceptedAtAction(nameof(GetJobStatus), new { jobId }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting PDF generation job");
            return StatusCode(500, new { error = "An error occurred while submitting the job" });
        }
    }

    /// <summary>
    /// Gets the status of a PDF generation job.
    /// </summary>
    /// <param name="jobId">The unique job identifier.</param>
    /// <returns>Job status and result if completed.</returns>
    /// <response code="200">Returns the job status. If completed, PDF can be downloaded.</response>
    /// <response code="404">If the job is not found.</response>
    /// <response code="401">If user is not authenticated.</response>
    [HttpGet("jobs/{jobId}")]
    [ProducesResponseType(typeof(PdfGenerationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> GetJobStatus(string jobId)
    {
        var userId = GetUserId();
        var job = await _jobService.GetJobAsync(jobId, userId);

        if (job == null)
        {
            return NotFound(new { error = "Job not found" });
        }

        var response = new PdfGenerationResponse
        {
            JobId = job.JobId,
            Status = job.Status.ToString(),
            Message = job.Status switch
            {
                Data.Entities.JobStatus.Pending => "Job is waiting to be processed",
                Data.Entities.JobStatus.Processing => "Job is currently being processed",
                Data.Entities.JobStatus.Completed => "Job completed successfully. Download the PDF using /pdf/jobs/{jobId}/download. Email has been sent.",
                Data.Entities.JobStatus.Failed => $"Job failed: {job.ErrorMessage}",
                Data.Entities.JobStatus.Cancelled => "Job was cancelled",
                _ => "Unknown status"
            },
            CreatedAt = job.CreatedAt
        };

        return Ok(response);
    }

    /// <summary>
    /// Downloads the generated PDF for a completed job.
    /// </summary>
    /// <param name="jobId">The unique job identifier.</param>
    /// <returns>The generated PDF file.</returns>
    /// <response code="200">Returns the PDF file.</response>
    /// <response code="404">If the job is not found.</response>
    /// <response code="400">If the job is not yet completed.</response>
    /// <response code="401">If user is not authenticated.</response>
    [HttpGet("jobs/{jobId}/download")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> DownloadPdf(string jobId)
    {
        var userId = GetUserId();
        var job = await _jobService.GetJobAsync(jobId, userId);

        if (job == null)
        {
            return NotFound(new { error = "Job not found" });
        }

        if (job.Status != Data.Entities.JobStatus.Completed)
        {
            return BadRequest(new { error = $"Job is not completed. Current status: {job.Status}" });
        }

        var pdfBytes = await _jobService.GetPdfBytesAsync(jobId, userId);

        if (pdfBytes == null)
        {
            return StatusCode(500, new { error = "PDF data is not available" });
        }

        var filename = job.Filename ?? "document.pdf";
        
        if (!filename.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            filename += ".pdf";
        }

        return File(pdfBytes, Constants.PDF, filename);
    }

    /// <summary>
    /// Cancels a pending PDF generation job.
    /// </summary>
    /// <param name="jobId">The unique job identifier.</param>
    /// <returns>Cancellation result.</returns>
    /// <response code="200">If the job was successfully cancelled.</response>
    /// <response code="400">If the job cannot be cancelled (already processing or completed).</response>
    /// <response code="404">If the job is not found.</response>
    /// <response code="401">If user is not authenticated.</response>
    [HttpDelete("jobs/{jobId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> CancelJob(string jobId)
    {
        var userId = GetUserId();
        var cancelled = await _jobService.CancelJobAsync(jobId, userId);

        if (!cancelled)
        {
            return BadRequest(new { error = "Job cannot be cancelled. It may be already processing or completed." });
        }

        return Ok(new { message = "Job cancelled successfully" });
    }
}
