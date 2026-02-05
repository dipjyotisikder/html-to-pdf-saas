using FluentValidation;
using HTPDF.Infrastructure.Common;
using HTPDF.Infrastructure.Database;
using HTPDF.Infrastructure.Database.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HTPDF.Features.Pdf.GetStatus;

public class GetStatusHandler : IRequestHandler<GetStatusQuery, Result<GetStatusResult>>
{
    private readonly ApplicationDbContext _context;
    private readonly IValidator<GetStatusQuery> _validator;

    public GetStatusHandler(ApplicationDbContext context, IValidator<GetStatusQuery> validator)
    {
        _context = context;
        _validator = validator;
    }

    public async Task<Result<GetStatusResult>> Handle(GetStatusQuery request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<GetStatusResult>.Failure(validationResult.Errors.First().ErrorMessage);
        }

        var job = await _context.PdfJobs
            .FirstAsync(j => j.JobId == request.JobId && j.UserId == request.UserId, cancellationToken);

        var message = job.Status switch
        {
            JobStatus.Pending => "Job Is Waiting To Be Processed",
            JobStatus.Processing => "Job Is Currently Being Processed",
            JobStatus.Completed => "Job Completed Successfully. Download The PDF Using /pdf/jobs/{jobId}/download. Email Has Been Sent.",
            JobStatus.Failed => $"Job Failed: {job.ErrorMessage}",
            JobStatus.Cancelled => "Job Was Cancelled",
            _ => "Unknown Status"
        };

        var result = new GetStatusResult(
            job.JobId,
            job.Status.ToString(),
            message,
            job.CreatedAt,
            job.CompletedAt
        );

        return Result<GetStatusResult>.Success(result);
    }
}

