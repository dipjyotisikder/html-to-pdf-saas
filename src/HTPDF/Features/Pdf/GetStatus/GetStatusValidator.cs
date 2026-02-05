using FluentValidation;
using HTPDF.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace HTPDF.Features.Pdf.GetStatus;

public class GetStatusValidator : AbstractValidator<GetStatusQuery>
{
    private readonly ApplicationDbContext _context;

    public GetStatusValidator(ApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.JobId)
            .NotEmpty().WithMessage("JobId Is Required")
            .MustAsync(BeAvailableJob).WithMessage("Job Not Found Or Not Owned By You");
    }

    private async Task<bool> BeAvailableJob(GetStatusQuery query, string jobId, CancellationToken cancellationToken)
    {
        return await _context.PdfJobs.AnyAsync(j => j.JobId == jobId && j.UserId == query.UserId, cancellationToken);
    }
}
