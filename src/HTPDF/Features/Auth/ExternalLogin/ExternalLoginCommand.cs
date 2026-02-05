using HTPDF.Features.Auth.Register;
using MediatR;

namespace HTPDF.Features.Auth.ExternalLogin;

public record ExternalLoginCommand(
    string Provider,
    string Email,
    string ExternalId,
    string? FirstName,
    string? LastName
) : IRequest<ExternalLoginResult>;

public record ExternalLoginResult(
    bool Success,
    string Message,
    AuthTokens? Tokens
);
