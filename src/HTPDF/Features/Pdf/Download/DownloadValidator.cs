using FluentValidation;
using HTPDF.Infrastructure.Database;
using HTPDF.Infrastructure.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace HTPDF.Features.Pdf.Download;

public class DownloadValidator : AbstractValidator<DownloadQuery>
{
    private readonly ApplicationDbContext _context;

    public DownloadValidator(ApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.JobId)
            .NotEmpty().WithMessage("JobId Is Required")
            .MustAsync(BeReadyForDownload).WithMessage("Job Not Found, Not Owned By You, Or Not Yet Completed");
    }

    private async Task<bool> BeReadyForDownload(DownloadQuery query, string jobId, CancellationToken cancellationToken)
    {
        return await _context.PdfJobs.AnyAsync(j =>
            j.JobId == jobId &&
            j.UserId == query.UserId &&
            j.Status == JobStatus.Completed &&
            !string.IsNullOrEmpty(j.FilePath),
            cancellationToken);
    }
}
