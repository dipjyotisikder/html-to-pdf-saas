using MediatR;

namespace HTPDF.Features.Pdf.GetUserJobs;

public record GetUserJobsQuery(
    string UserId,
    int PageNumber = 1,
    int PageSize = 20,
    string? Status = null
) : IRequest<GetUserJobsResult>;

public record GetUserJobsResult(
    List<JobSummary> Jobs,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages
);

public record JobSummary(
    string JobId,
    string Filename,
    string Status,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    DateTime? ExpiresAt,
    bool IsExpired,
    bool CanDownload,
    string? ErrorMessage
);
