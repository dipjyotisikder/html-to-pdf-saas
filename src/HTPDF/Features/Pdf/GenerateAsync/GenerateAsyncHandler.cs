using Ganss.Xss;
using HTPDF.Infrastructure.Database;
using HTPDF.Infrastructure.Database.Entities;
using HTPDF.Infrastructure.Logging;
using MediatR;

using System.Threading.Channels;

namespace HTPDF.Features.Pdf.GenerateAsync;

public class GenerateAsyncHandler : IRequestHandler<GenerateAsyncCommand, GenerateAsyncResult>
{
    private readonly ApplicationDbContext _context;
    private readonly HtmlSanitizer _sanitizer;
    private readonly Channel<string> _jobQueue;
    private readonly ILoggingService<GenerateAsyncHandler> _logger;
    private readonly int _retentionDays;

    public GenerateAsyncHandler(
        ApplicationDbContext context,
        HtmlSanitizer sanitizer,
        Channel<string> jobQueue,
        IConfiguration configuration,
        ILoggingService<GenerateAsyncHandler> logger)

    {
        _context = context;
        _sanitizer = sanitizer;
        _jobQueue = jobQueue;
        _logger = logger;
        _retentionDays = configuration.GetValue<int>("FileStorageSettings:RetentionDays", 7);
    }

    public async Task<GenerateAsyncResult> Handle(GenerateAsyncCommand request, CancellationToken cancellationToken)
    {
        var sanitizedHtml = _sanitizer.Sanitize(request.HtmlContent);
        var jobId = Guid.NewGuid().ToString("N");

        var job = new PdfJobEntity
        {
            JobId = jobId,
            UserId = request.UserId,
            HtmlContent = sanitizedHtml,
            Orientation = request.Orientation,
            PaperSize = request.PaperSize,
            Filename = request.Filename ?? "Document.pdf",
            Status = JobStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(_retentionDays)
        };

        _context.PdfJobs.Add(job);
        await _context.SaveChangesAsync(cancellationToken);

        await _jobQueue.Writer.WriteAsync(jobId, cancellationToken);

        _logger.LogInfo(LogMessages.Pdf.JobCreated, jobId, request.UserId);

        return new GenerateAsyncResult(

            jobId,
            "Pending",
            "PDF Generation Job Has Been Queued. You Will Receive An Email When It's Ready.",
            DateTime.UtcNow
        );
    }
}
