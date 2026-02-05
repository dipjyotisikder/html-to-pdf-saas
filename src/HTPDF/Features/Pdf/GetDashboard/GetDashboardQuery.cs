using HTPDF.Infrastructure.Common;
using MediatR;

namespace HTPDF.Features.Pdf.GetDashboard;

public record GetDashboardQuery(
    string UserId
) : IRequest<Result<DashboardResult>>;


public record DashboardResult(
    UserStats Stats,
    List<RecentJob> RecentJobs
);

public record UserStats(
    int TotalJobs,
    int CompletedJobs,
    int PendingJobs,
    int FailedJobs,
    int AvailableDownloads
);

public record RecentJob(
    string JobId,
    string Filename,
    string Status,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    bool CanDownload
);
