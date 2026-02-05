using HTPDF.Infrastructure.Database;
using HTPDF.Infrastructure.Database.Entities;
using HTPDF.Infrastructure.Storage;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HTPDF.Features.Pdf.Download;

public class DownloadHandler : IRequestHandler<DownloadQuery, DownloadResult?>
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorage _fileStorage;

    public DownloadHandler(ApplicationDbContext context, IFileStorage fileStorage)
    {
        _context = context;
        _fileStorage = fileStorage;
    }

    public async Task<DownloadResult?> Handle(DownloadQuery request, CancellationToken cancellationToken)
    {
        var job = await _context.PdfJobs
            .Where(j => j.JobId == request.JobId && j.UserId == request.UserId && j.Status == JobStatus.Completed)
            .FirstOrDefaultAsync(cancellationToken);

        if (job == null || string.IsNullOrEmpty(job.FilePath))
        {
            return null;
        }

        var pdfBytes = await _fileStorage.ReadAsync(job.FilePath, cancellationToken);

        if (pdfBytes == null)
        {
            return null;
        }

        return new DownloadResult(pdfBytes, job.Filename);
    }
}
