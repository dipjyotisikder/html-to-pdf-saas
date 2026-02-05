using HTPDF.Models;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace HTPDF.Services;

/// <summary>
/// Service for managing PDF generation jobs asynchronously.
/// </summary>
public interface IPdfJobService
{
    /// <summary>
    /// Submits a new PDF generation job.
    /// </summary>
    Task<string> SubmitJobAsync(PdfGenerationRequest request);

    /// <summary>
    /// Gets the status and result of a PDF generation job.
    /// </summary>
    Task<PdfJob?> GetJobAsync(string jobId);

    /// <summary>
    /// Cancels a pending job.
    /// </summary>
    Task<bool> CancelJobAsync(string jobId);
}

/// <summary>
/// Background service that processes PDF generation jobs.
/// </summary>
public class PdfJobService : BackgroundService, IPdfJobService
{
    private readonly IPdfMaker _pdfMaker;
    private readonly ILogger<PdfJobService> _logger;
    private readonly ConcurrentDictionary<string, PdfJob> _jobs = new();
    private readonly Channel<string> _jobQueue = Channel.CreateUnbounded<string>();

    public PdfJobService(IPdfMaker pdfMaker, ILogger<PdfJobService> logger)
    {
        _pdfMaker = pdfMaker;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> SubmitJobAsync(PdfGenerationRequest request)
    {
        var jobId = Guid.NewGuid().ToString("N");
        
        var job = new PdfJob
        {
            JobId = jobId,
            Status = JobStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            Filename = request.Filename ?? "document.pdf"
        };

        _jobs[jobId] = job;

        // Store the request temporarily in a dictionary (in production, use proper storage)
        _pendingRequests[jobId] = request;

        await _jobQueue.Writer.WriteAsync(jobId);

        _logger.LogInformation("Job {JobId} submitted for processing", jobId);

        return jobId;
    }

    /// <inheritdoc />
    public Task<PdfJob?> GetJobAsync(string jobId)
    {
        _jobs.TryGetValue(jobId, out var job);
        return Task.FromResult(job);
    }

    /// <inheritdoc />
    public Task<bool> CancelJobAsync(string jobId)
    {
        if (_jobs.TryGetValue(jobId, out var job) && job.Status == JobStatus.Pending)
        {
            job.Status = JobStatus.Failed;
            job.ErrorMessage = "Job was cancelled by user";
            job.CompletedAt = DateTime.UtcNow;
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    private readonly ConcurrentDictionary<string, PdfGenerationRequest> _pendingRequests = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PDF Job Service started");

        await foreach (var jobId in _jobQueue.Reader.ReadAllAsync(stoppingToken))
        {
            if (!_jobs.TryGetValue(jobId, out var job))
            {
                continue;
            }

            if (!_pendingRequests.TryRemove(jobId, out var request))
            {
                continue;
            }

            try
            {
                job.Status = JobStatus.Processing;
                _logger.LogInformation("Processing job {JobId}", jobId);

                var pdfBytes = await Task.Run(() => 
                    _pdfMaker.CreatePDF(request.HtmlContent, request.Orientation, request.PaperSize), 
                    stoppingToken);

                job.PdfBytes = pdfBytes;
                job.Status = JobStatus.Completed;
                job.CompletedAt = DateTime.UtcNow;

                _logger.LogInformation("Job {JobId} completed successfully", jobId);
            }
            catch (Exception ex)
            {
                job.Status = JobStatus.Failed;
                job.ErrorMessage = ex.Message;
                job.CompletedAt = DateTime.UtcNow;

                _logger.LogError(ex, "Job {JobId} failed", jobId);
            }
        }
    }
}
