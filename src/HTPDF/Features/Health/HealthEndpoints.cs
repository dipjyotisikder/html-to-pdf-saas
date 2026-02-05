namespace HTPDF.Features.Health;

public static class HealthEndpoints
{
    public static RouteGroupBuilder MapHealthEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/health")
            .WithTags("Health")
            .AllowAnonymous();

        group.MapGet("", () => Results.Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "v3.0",
            Framework = ".NET 10"
        }))
        .WithName("HealthCheck")
        .WithSummary("Check API health status");

        return group;
    }
}
