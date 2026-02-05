using FluentValidation;
using HTPDF.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace HTPDF.Features.Auth.RefreshTokens;

public class RefreshTokenValidator : AbstractValidator<RefreshTokenCommand>
{
    private readonly ApplicationDbContext _context;

    public RefreshTokenValidator(ApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.AccessToken)
            .NotEmpty()
            .WithMessage("ACCESS TOKEN IS REQUIRED");

        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("REFRESH TOKEN IS REQUIRED")
            .MustAsync(BeValidToken).WithMessage("Refresh Token Is Invalid, Already Used, Or Expired");
    }

    private async Task<bool> BeValidToken(string token, CancellationToken cancellationToken)
    {
        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == token, cancellationToken);

        return storedToken != null && !storedToken.IsUsed && !storedToken.IsRevoked && storedToken.ExpiresAt > DateTime.UtcNow;
    }
}

