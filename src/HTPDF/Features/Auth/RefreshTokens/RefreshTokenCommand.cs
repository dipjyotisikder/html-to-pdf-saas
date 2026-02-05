using HTPDF.Features.Auth.Register;
using HTPDF.Infrastructure.Common;
using MediatR;

namespace HTPDF.Features.Auth.RefreshTokens;

public record RefreshTokenCommand(
    string AccessToken,
    string RefreshToken
) : IRequest<Result<AuthTokens>>;

