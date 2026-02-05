using HTPDF.Infrastructure.Database;
using HTPDF.Infrastructure.Database.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HTPDF.Features.Pdf.GetStatus;

public class GetStatusHandler : IRequestHandler<GetStatusQuery, GetStatusResult?>
{
    private readonly ApplicationDbContext _context;

    public GetStatusHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<GetStatusResult?> Handle(GetStatusQuery request, CancellationToken cancellationToken)
    {
        var job = await _context.PdfJobs
            .Where(j => j.JobId == request.JobId && j.UserId == request.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (job == null)
        {
            return null;
        }

        var message = job.Status switch
        {
            JobStatus.Pending => "Job Is Waiting To Be Processed",
            JobStatus.Processing => "Job Is Currently Being Processed",
            JobStatus.Completed => "Job Completed Successfully. Download The PDF Using /pdf/jobs/{jobId}/download. Email Has Been Sent.",
            JobStatus.Failed => $"Job Failed: {job.ErrorMessage}",
            JobStatus.Cancelled => "Job Was Cancelled",
            _ => "Unknown Status"
        };

        return new GetStatusResult(
            job.JobId,
            job.Status.ToString(),
            message,
            job.CreatedAt,
            job.CompletedAt
        );
    }
}
