using HTPDF.Infrastructure.Database;
using HTPDF.Infrastructure.Database.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HTPDF.Features.Pdf.GetDashboard;

public class GetDashboardHandler : IRequestHandler<GetDashboardQuery, DashboardResult>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GetDashboardHandler> _logger;

    public GetDashboardHandler(
        ApplicationDbContext context,
        ILogger<GetDashboardHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DashboardResult> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        var userJobs = await _context.PdfJobs
            .Where(j => j.UserId == request.UserId)
            .ToListAsync(cancellationToken);

        var stats = new UserStats(
            TotalJobs: userJobs.Count,
            CompletedJobs: userJobs.Count(j => j.Status == JobStatus.Completed),
            PendingJobs: userJobs.Count(j => j.Status == JobStatus.Pending || j.Status == JobStatus.Processing),
            FailedJobs: userJobs.Count(j => j.Status == JobStatus.Failed),
            AvailableDownloads: userJobs.Count(j =>
                j.Status == JobStatus.Completed &&
                j.ExpiresAt.HasValue &&
                j.ExpiresAt.Value > DateTime.UtcNow &&
                !string.IsNullOrEmpty(j.FilePath))
        );

        var recentJobs = userJobs
            .OrderByDescending(j => j.CreatedAt)
            .Take(10)
            .Select(j => new RecentJob(
                j.JobId,
                j.Filename,
                j.Status.ToString(),
                j.CreatedAt,
                j.CompletedAt,
                j.Status == JobStatus.Completed &&
                    j.ExpiresAt.HasValue &&
                    j.ExpiresAt.Value > DateTime.UtcNow &&
                    !string.IsNullOrEmpty(j.FilePath)
            ))
            .ToList();

        _logger.LogInformation("Dashboard Loaded For User {UserId} - {TotalJobs} Total Jobs",
            request.UserId, stats.TotalJobs);

        return new DashboardResult(stats, recentJobs);
    }
}
