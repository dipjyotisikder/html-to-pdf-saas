using HTPDF.Features.Auth.ExternalLogin;
using HTPDF.Features.Auth.Login;
using HTPDF.Features.Auth.RefreshTokens;
using HTPDF.Features.Auth.Register;
using HTPDF.Infrastructure.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HTPDF.Features.Auth;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/auth")
            .WithTags("Authentication");

        group.MapPost("/register", async Task<IResult> ([FromBody] RegisterCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);
            return result.IsSuccess
                ? Results.Ok(new { Message = result.Message })
                : Results.BadRequest(new { Error = result.Message });
        })
        .AllowAnonymous()
        .WithName("Register")
        .WithSummary("Register a new user");

        group.MapPost("/login", async Task<IResult> ([FromBody] LoginCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { Error = result.Message });
        })
        .AllowAnonymous()
        .WithName("Login")
        .WithSummary("Login with email and password");

        group.MapPost("/refresh", async Task<IResult> ([FromBody] RefreshTokenCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { Error = result.Message });
        })
        .AllowAnonymous()
        .WithName("RefreshToken")
        .WithSummary("Refresh access token");

        group.MapPost("/external-login", async Task<IResult> ([FromBody] ExternalLoginCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { Error = result.Message });
        })
        .AllowAnonymous()
        .WithName("ExternalLogin")
        .WithSummary("Login with external provider token");

        return group;
    }
}
