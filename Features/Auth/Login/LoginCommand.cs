using HTPDF.Features.Auth.Register;
using MediatR;

namespace HTPDF.Features.Auth.Login;

public record LoginCommand(
    string Email,
    string Password
) : IRequest<LoginResult>;

public record LoginResult(
    bool Success,
    string Message,
    AuthTokens? Tokens
);
