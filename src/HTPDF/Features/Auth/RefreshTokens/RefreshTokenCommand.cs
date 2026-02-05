using HTPDF.Features.Auth.Register;
using MediatR;

namespace HTPDF.Features.Auth.RefreshTokens;

public record RefreshTokenCommand(
    string AccessToken,
    string RefreshToken
) : IRequest<RefreshTokenResult>;

public record RefreshTokenResult(
    bool Success,
    string Message,
    AuthTokens? Tokens
);
