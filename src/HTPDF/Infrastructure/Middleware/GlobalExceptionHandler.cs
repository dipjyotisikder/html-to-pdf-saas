using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace HTPDF.Infrastructure.Middleware;

public class GlobalExceptionHandler : IMiddleware
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation Error");

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";

            var problemDetails = new ValidationProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = "One Or More Validation Errors Occurred"
            };

            foreach (var error in ex.Errors)
            {
                problemDetails.Errors.Add(error.PropertyName, new[] { error.ErrorMessage });
            }

            await context.Response.WriteAsJsonAsync(problemDetails);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized Access");

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new { Error = "Unauthorized Access" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled Exception");

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new { Error = "An Error Occurred While Processing Your Request" });
        }
    }
}
