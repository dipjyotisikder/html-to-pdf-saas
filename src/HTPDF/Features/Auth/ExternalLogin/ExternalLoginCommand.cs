using HTPDF.Features.Auth.Register;
using HTPDF.Infrastructure.Common;
using MediatR;

namespace HTPDF.Features.Auth.ExternalLogin;

public record ExternalLoginCommand(
    string Provider,
    string Email,
    string ExternalId,
    string? FirstName,
    string? LastName
) : IRequest<Result<AuthTokens>>;

