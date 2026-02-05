using MediatR;

namespace HTPDF.Features.Auth.Register;

public record RegisterCommand(
    string Email,
    string Password,
    string ConfirmPassword,
    string? FirstName,
    string? LastName
) : IRequest<RegisterResult>;

public record RegisterResult(
    bool Success,
    string Message,
    AuthTokens? Tokens
);

public record AuthTokens(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string Email,
    List<string> Roles
);
