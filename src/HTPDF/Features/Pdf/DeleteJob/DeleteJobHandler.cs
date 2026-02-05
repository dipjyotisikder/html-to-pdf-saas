using HTPDF.Infrastructure.Database;
using HTPDF.Infrastructure.Storage;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HTPDF.Features.Pdf.DeleteJob;

public class DeleteJobHandler : IRequestHandler<DeleteJobCommand, DeleteJobResult>
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<DeleteJobHandler> _logger;

    public DeleteJobHandler(
        ApplicationDbContext context,
        IFileStorage fileStorage,
        ILogger<DeleteJobHandler> logger)
    {
        _context = context;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task<DeleteJobResult> Handle(DeleteJobCommand request, CancellationToken cancellationToken)
    {
        var job = await _context.PdfJobs
            .FirstOrDefaultAsync(j => j.JobId == request.JobId && j.UserId == request.UserId, cancellationToken);

        if (job == null)
        {
            return new DeleteJobResult(false, "Job Not Found");
        }

        if (!string.IsNullOrEmpty(job.FilePath))
        {
            try
            {
                await _fileStorage.DeleteAsync(job.FilePath, cancellationToken);
                _logger.LogInformation("Deleted PDF File {FilePath} For Job {JobId}", job.FilePath, job.JobId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed To Delete PDF File {FilePath} For Job {JobId}", job.FilePath, job.JobId);
            }
        }

        _context.PdfJobs.Remove(job);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Job {JobId} Deleted By User {UserId}", request.JobId, request.UserId);

        return new DeleteJobResult(true, "Job Deleted Successfully");
    }
}
