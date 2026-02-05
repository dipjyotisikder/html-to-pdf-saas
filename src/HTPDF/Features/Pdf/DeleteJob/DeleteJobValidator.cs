using FluentValidation;
using HTPDF.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace HTPDF.Features.Pdf.DeleteJob;

public class DeleteJobValidator : AbstractValidator<DeleteJobCommand>
{
    private readonly ApplicationDbContext _context;

    public DeleteJobValidator(ApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.JobId)
            .NotEmpty().WithMessage("JobId Is Required")
            .MustAsync(BeValidAndOwnedJob).WithMessage("Job Not Found Or You Do Not Have Permission To Delete It");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId Is Required");
    }

    private async Task<bool> BeValidAndOwnedJob(DeleteJobCommand command, string jobId, CancellationToken cancellationToken)
    {
        return await _context.PdfJobs.AnyAsync(j => j.JobId == jobId && j.UserId == command.UserId, cancellationToken);
    }
}
