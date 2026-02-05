using DinkToPdf;
using DinkToPdf.Contracts;
using HTPDF.Infrastructure.Database;
using HTPDF.Infrastructure.Database.Entities;
using HTPDF.Infrastructure.Logging;
using HTPDF.Infrastructure.Storage;

using Microsoft.EntityFrameworkCore;
using System.Threading.Channels;

namespace HTPDF.Infrastructure.BackgroundJobs;

public class PdfJobProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Channel<string> _jobQueue;
    private readonly ILoggingService<PdfJobProcessor> _logger;

    public PdfJobProcessor(
        IServiceScopeFactory scopeFactory,
        Channel<string> jobQueue,
        ILoggingService<PdfJobProcessor> logger)

    {
        _scopeFactory = scopeFactory;
        _jobQueue = jobQueue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInfo(LogMessages.Infrastructure.PdfJobProcessorStarted);

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
        var converter = scope.ServiceProvider.GetRequiredService<IConverter>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorage>();

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

            _logger.LogInfo(LogMessages.Infrastructure.ProcessingPdfJob, jobId, job.AttemptCount);

            var pdfBytes = await Task.Run(() => ConvertHtmlToPdf(converter, job.HtmlContent, job.Orientation, job.PaperSize), cancellationToken);

            var filePath = await fileStorage.SaveAsync(pdfBytes, job.Filename, cancellationToken);
            job.FilePath = filePath;
            job.Status = JobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInfo(LogMessages.Infrastructure.PdfJobCompleted, jobId);

            await CreateOutboxMessageAsync(context, job, cancellationToken);
        }
        catch (Exception ex)
        {
            job.Status = JobStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.CompletedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            _logger.LogError(ex, LogMessages.Infrastructure.PdfJobFailed, jobId);

            await CreateFailureOutboxMessageAsync(context, job, cancellationToken);
        }
    }


    private static byte[] ConvertHtmlToPdf(IConverter converter, string htmlContent, string orientation, string paperSize)
    {
        var orientationEnum = orientation.Equals("Landscape", StringComparison.OrdinalIgnoreCase)
            ? Orientation.Landscape
            : Orientation.Portrait;

        var paperKind = paperSize.ToUpperInvariant() switch
        {
            "A3" => PaperKind.A3,
            "LETTER" => PaperKind.Letter,
            "LEGAL" => PaperKind.Legal,
            _ => PaperKind.A4
        };

        var globalSettings = new GlobalSettings
        {
            ColorMode = ColorMode.Color,
            Orientation = orientationEnum,
            PaperSize = paperKind,
            Margins = new MarginSettings { Top = 10, Bottom = 10, Left = 10, Right = 10 },
            DocumentTitle = "PDF"
        };

        var objectSettings = new ObjectSettings
        {
            HtmlContent = htmlContent,
            WebSettings = { DefaultEncoding = "utf-8" }
        };

        var document = new HtmlToPdfDocument
        {
            GlobalSettings = globalSettings,
            Objects = { objectSettings }
        };

        return converter.Convert(document);
    }

    private static async Task CreateOutboxMessageAsync(ApplicationDbContext context, PdfJobEntity job, CancellationToken cancellationToken)
    {
        var message = new OutboxMessage
        {
            MessageType = "PdfCompleted",
            JobId = job.JobId,
            UserId = job.UserId,
            EmailTo = job.User!.Email!,
            EmailSubject = "Your PDF Is Ready!",
            EmailBody = $"Your PDF '{job.Filename}' Has Been Generated Successfully.",
            AttachmentFilename = job.Filename
        };

        context.OutboxMessages.Add(message);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task CreateFailureOutboxMessageAsync(ApplicationDbContext context, PdfJobEntity job, CancellationToken cancellationToken)
    {
        var message = new OutboxMessage
        {
            MessageType = "PdfFailed",
            JobId = job.JobId,
            UserId = job.UserId,
            EmailTo = job.User!.Email!,
            EmailSubject = "PDF Generation Failed",
            EmailBody = $"Your PDF Generation Failed With Error: {job.ErrorMessage}"
        };

        context.OutboxMessages.Add(message);
        await context.SaveChangesAsync(cancellationToken);
    }
}
