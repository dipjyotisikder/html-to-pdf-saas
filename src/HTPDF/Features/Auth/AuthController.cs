using HTPDF.Features.Auth.ExternalLogin;
using HTPDF.Features.Auth.Login;
using HTPDF.Features.Auth.RefreshTokens;
using HTPDF.Features.Auth.Register;
using HTPDF.Infrastructure.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HTPDF.Features.Auth;

[Route("auth")]
public class AuthController : BaseApiController
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult> Register([FromBody] RegisterCommand command)
    {
        var result = await _mediator.Send(command);
        return HandleResult(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult> Login([FromBody] LoginCommand command)
    {
        var result = await _mediator.Send(command);
        return HandleResult(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult> RefreshToken([FromBody] RefreshTokenCommand command)
    {
        var result = await _mediator.Send(command);
        return HandleResult(result);
    }

    [HttpPost("external-login")]
    [AllowAnonymous]
    public async Task<ActionResult> ExternalLogin([FromBody] ExternalLoginCommand command)
    {
        var result = await _mediator.Send(command);
        return HandleResult(result);
    }
}

