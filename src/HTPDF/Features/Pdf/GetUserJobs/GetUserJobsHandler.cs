using FluentValidation;
using HTPDF.Infrastructure.Database;
using HTPDF.Infrastructure.Database.Entities;
using HTPDF.Infrastructure.Logging;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HTPDF.Features.Pdf.GetUserJobs;

public class GetUserJobsHandler : IRequestHandler<GetUserJobsQuery, GetUserJobsResult>
{
    private readonly ApplicationDbContext _context;
    private readonly IValidator<GetUserJobsQuery> _validator;
    private readonly ILoggingService<GetUserJobsHandler> _logger;

    public GetUserJobsHandler(
        ApplicationDbContext context,
        IValidator<GetUserJobsQuery> validator,
        ILoggingService<GetUserJobsHandler> logger)

    {
        _context = context;
        _validator = validator;
        _logger = logger;
    }

    public async Task<GetUserJobsResult> Handle(GetUserJobsQuery request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var query = _context.PdfJobs

            .Where(j => j.UserId == request.UserId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(request.Status))
        {
            if (Enum.TryParse<JobStatus>(request.Status, true, out var statusEnum))
            {
                query = query.Where(j => j.Status == statusEnum);
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var jobs = await query
            .OrderByDescending(j => j.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(j => new JobSummary(
                j.JobId,
                j.Filename,
                j.Status.ToString(),
                j.CreatedAt,
                j.CompletedAt,
                j.ExpiresAt,
                j.ExpiresAt.HasValue && j.ExpiresAt.Value < DateTime.UtcNow,
                j.Status == JobStatus.Completed &&
                    j.ExpiresAt.HasValue &&
                    j.ExpiresAt.Value > DateTime.UtcNow &&
                    !string.IsNullOrEmpty(j.FilePath),
                j.ErrorMessage
            ))
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        _logger.LogInfo(LogMessages.Pdf.UserJobsRetrieved, jobs.Count, request.UserId);

        return new GetUserJobsResult(

            jobs,
            totalCount,
            request.PageNumber,
            request.PageSize,
            totalPages
        );
    }
}
