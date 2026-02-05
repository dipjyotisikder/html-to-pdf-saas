using HTPDF.Features.Auth.Register;
using HTPDF.Infrastructure.Common;
using MediatR;

namespace HTPDF.Features.Auth.Login;

public record LoginCommand(
    string Email,
    string Password
) : IRequest<Result<AuthTokens>>;

