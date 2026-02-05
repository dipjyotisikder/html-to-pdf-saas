using HTPDF.Features.Pdf.DeleteJob;
using HTPDF.Features.Pdf.Download;
using HTPDF.Features.Pdf.GenerateAsync;
using HTPDF.Features.Pdf.GetDashboard;
using HTPDF.Features.Pdf.GetStatus;
using HTPDF.Features.Pdf.GetUserJobs;
using MediatR;
using System.Security.Claims;

namespace HTPDF.Features.Pdf;

public static class PdfEndpoints
{
    public static RouteGroupBuilder MapPdfEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/pdf")
            .WithTags("PDF Generation")
            .RequireAuthorization();

        group.MapGet("/dashboard", async Task<IResult> (ClaimsPrincipal user, IMediator mediator) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var query = new GetDashboardQuery(userId);
            var result = await mediator.Send(query);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { Error = result.Message });
        })
        .WithName("GetDashboard")
        .WithSummary("Get user PDF generation dashboard");

        group.MapPost("/generate/async", async Task<IResult> (
            [FromBody] GenerateAsyncRequest request,
            ClaimsPrincipal user,
            IMediator mediator,
            HttpContext httpContext) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var userEmail = user.FindFirstValue(ClaimTypes.Email)!;

            var command = new GenerateAsyncCommand(
                userId,
                userEmail,
                request.HtmlContent,
                request.Filename,
                request.Orientation ?? "Portrait",
                request.PaperSize ?? "A4"
            );

            var result = await mediator.Send(command);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new { Error = result.Message });
            }

            var location = $"/pdf/jobs/{result.Value!.JobId}";
            return Results.Accepted(location, result.Value);
        })
        .WithName("GenerateAsync")
        .WithSummary("Generate PDF asynchronously");

        group.MapGet("/jobs", async Task<IResult> (
            ClaimsPrincipal user,
            IMediator mediator,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var query = new GetUserJobsQuery(userId, pageNumber, pageSize, status);
            var result = await mediator.Send(query);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { Error = result.Message });
        })
        .WithName("GetUserJobs")
        .WithSummary("Get user's PDF generation jobs");

        group.MapGet("/jobs/{jobId}", async Task<IResult> (string jobId, ClaimsPrincipal user, IMediator mediator) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var query = new GetStatusQuery(jobId, userId);
            var result = await mediator.Send(query);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { Error = result.Message });
        })
        .WithName("GetStatus")
        .WithSummary("Get PDF generation job status");

        group.MapGet("/jobs/{jobId}/download", async Task<IResult> (string jobId, ClaimsPrincipal user, IMediator mediator) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var query = new DownloadQuery(jobId, userId);
            var result = await mediator.Send(query);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new { Error = result.Message });
            }

            var fileResult = result.Value!;
            return Results.File(
                fileResult.PdfBytes,
                "application/pdf",
                fileResult.Filename
            );
        })
        .WithName("DownloadPdf")
        .WithSummary("Download generated PDF file");

        group.MapDelete("/jobs/{jobId}", async Task<IResult> (string jobId, ClaimsPrincipal user, IMediator mediator) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var command = new DeleteJobCommand(jobId, userId);
            var result = await mediator.Send(command);

            return result.IsSuccess
                ? Results.Ok(new { Message = result.Message })
                : Results.BadRequest(new { Error = result.Message });
        })
        .WithName("DeleteJob")
        .WithSummary("Delete a PDF generation job");

        return group;
    }
}
