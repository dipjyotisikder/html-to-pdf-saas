using FluentValidation;
using HTPDF.Infrastructure.Database;
using HTPDF.Infrastructure.Database.Entities;
using HTPDF.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HTPDF.Features.Pdf.GenerateAsync;

public class GenerateAsyncValidator : AbstractValidator<GenerateAsyncCommand>
{
    private readonly ApplicationDbContext _context;
    private readonly RequestLimits _limits;

    public GenerateAsyncValidator(ApplicationDbContext context, IOptions<RequestLimits> options)
    {
        _context = context;
        _limits = options.Value;

        RuleFor(x => x.HtmlContent)
            .NotEmpty().WithMessage("HTML Content Is Required")
            .MaximumLength((int)_limits.MaxHtmlSizeBytes).WithMessage($"HTML Content Cannot Exceed {_limits.MaxHtmlSizeBytes / 1024 / 1024}MB");

        RuleFor(x => x.Filename)
            .MaximumLength(255).WithMessage("Filename Cannot Exceed 255 Characters");

        RuleFor(x => x.Orientation)
            .Must(x => x == "Portrait" || x == "Landscape")
            .WithMessage("Orientation Must Be Either Portrait Or Landscape");

        RuleFor(x => x.PaperSize)
            .Must(x => new[] { "A4", "A3", "LETTER", "LEGAL" }.Contains(x.ToUpperInvariant()))
            .WithMessage("Paper Size Must Be A4, A3, Letter, Or Legal");

        RuleFor(x => x)
            .MustAsync(HaveAllowedConcurrentJobs)
            .WithMessage($"You Have Exceeded The Maximum For Concurrent Jobs ({_limits.MaxConcurrentJobsPerKey})");
    }

    private async Task<bool> HaveAllowedConcurrentJobs(GenerateAsyncCommand command, CancellationToken cancellationToken)
    {
        var activeJobs = await _context.PdfJobs
            .CountAsync(j => j.UserId == command.UserId && (j.Status == JobStatus.Pending || j.Status == JobStatus.Processing), cancellationToken);

        return activeJobs < _limits.MaxConcurrentJobsPerKey;
    }
}

