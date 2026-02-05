using HTPDF.Models;
using HTPDF.Data;
using HTPDF.Data.Entities;
using HTPDF.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
    Task<string> SubmitJobAsync(PdfGenerationRequest request, string userId, string userEmail);

    /// <summary>
    /// Gets the status and result of a PDF generation job.
    /// </summary>
    Task<PdfJob?> GetJobAsync(string jobId, string userId);

    /// <summary>
    /// Cancels a pending job.
    /// </summary>
    Task<bool> CancelJobAsync(string jobId, string userId);
    
    /// <summary>
    /// Gets PDF bytes for a completed job.
    /// </summary>
    Task<byte[]?> GetPdfBytesAsync(string jobId, string userId);
}

/// <summary>
/// Background service that processes PDF generation jobs.
/// </summary>
public class PdfJobService : BackgroundService, IPdfJobService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PdfJobService> _logger;
    private readonly Channel<string> _jobQueue = Channel.CreateUnbounded<string>();
    private readonly FileStorageSettings _storageSettings;

    public PdfJobService(
        IServiceScopeFactory scopeFactory,
        IOptions<FileStorageSettings> storageSettings,
        ILogger<PdfJobService> logger)
    {
        _scopeFactory = scopeFactory;
        _storageSettings = storageSettings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> SubmitJobAsync(PdfGenerationRequest request, string userId, string userEmail)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var htmlSanitizer = scope.ServiceProvider.GetRequiredService<IHtmlSanitizerService>();

        var jobId = Guid.NewGuid().ToString("N");

        var job = new PdfJobEntity
        {
            JobId = jobId,
            UserId = userId,
            HtmlContent = htmlSanitizer.Sanitize(request.HtmlContent),
            Orientation = request.Orientation,
            PaperSize = request.PaperSize,
            Filename = request.Filename ?? "document.pdf",
            Status = JobStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(_storageSettings.RetentionDays)
        };

        context.PdfJobs.Add(job);
        await context.SaveChangesAsync();

        await _jobQueue.Writer.WriteAsync(jobId);

        _logger.LogInformation("Job {JobId} submitted for user {UserId}", jobId, userId);

        return jobId;
    }

    /// <inheritdoc />
    public async Task<PdfJob?> GetJobAsync(string jobId, string userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var jobEntity = await context.PdfJobs
            .Where(j => j.JobId == jobId && j.UserId == userId)
            .FirstOrDefaultAsync();

        if (jobEntity == null)
        {
            return null;
        }

        return new PdfJob
        {
            JobId = jobEntity.JobId,
            Status = jobEntity.Status,
            ErrorMessage = jobEntity.ErrorMessage,
            CreatedAt = jobEntity.CreatedAt,
            CompletedAt = jobEntity.CompletedAt,
            Filename = jobEntity.Filename
        };
    }

    /// <inheritdoc />
    public async Task<bool> CancelJobAsync(string jobId, string userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var job = await context.PdfJobs
            .Where(j => j.JobId == jobId && j.UserId == userId && j.Status == JobStatus.Pending)
            .FirstOrDefaultAsync();

        if (job == null)
        {
            return false;
        }

        job.Status = JobStatus.Cancelled;
        job.CompletedAt = DateTime.UtcNow;
        job.ErrorMessage = "Cancelled by user";

        await context.SaveChangesAsync();

        _logger.LogInformation("Job {JobId} cancelled by user {UserId}", jobId, userId);

        return true;
    }

    /// <inheritdoc />
    public async Task<byte[]?> GetPdfBytesAsync(string jobId, string userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();

        var job = await context.PdfJobs
            .Where(j => j.JobId == jobId && j.UserId == userId && j.Status == JobStatus.Completed)
            .FirstOrDefaultAsync();

        if (job == null || string.IsNullOrEmpty(job.FilePath))
        {
            return null;
        }

        return await fileStorage.GetPdfAsync(job.FilePath);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PDF Job Service started");

        await foreach (var jobId in _jobQueue.Reader.ReadAllAsync(stoppingToken))
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            _ = Task.Run(async () => await ProcessJobAsync(jobId, stoppingToken), stoppingToken);
        }
    }

    private async Task ProcessJobAsync(string jobId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var pdfMaker = scope.ServiceProvider.GetRequiredService<IPdfMaker>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();
        var outboxService = scope.ServiceProvider.GetRequiredService<IOutboxService>();

        var job = await context.PdfJobs
            .Include(j => j.User)
            .FirstOrDefaultAsync(j => j.JobId == jobId, cancellationToken);

        if (job == null || job.Status != JobStatus.Pending)
        {
            return;
        }

        try
        {
            job.Status = JobStatus.Processing;
            job.AttemptCount++;
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Processing PDF job {JobId}, Attempt {Attempt}", jobId, job.AttemptCount);

            // Generate PDF
            var pdfBytes = await Task.Run(() =>
                pdfMaker.CreatePDF(job.HtmlContent, job.Orientation, job.PaperSize),
                cancellationToken);

            // Save to file system
            var filePath = await fileStorage.SavePdfAsync(pdfBytes, job.Filename);
            job.FilePath = filePath;
            job.Status = JobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Job {JobId} completed successfully", jobId);

            // Create outbox message for email notification
            var downloadUrl = $"/pdf/jobs/{jobId}/download"; // Will be made absolute in email
            await outboxService.CreatePdfCompletedMessageAsync(
                jobId,
                job.UserId,
                job.User!.Email!,
                job.Filename,
                downloadUrl);
        }
        catch (Exception ex)
        {
            job.Status = JobStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.CompletedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            _logger.LogError(ex, "Job {JobId} failed", jobId);

            // Create outbox message for failure notification
            await outboxService.CreatePdfFailedMessageAsync(
                jobId,
                job.UserId,
                job.User!.Email!,
                ex.Message);
        }
    }
}
