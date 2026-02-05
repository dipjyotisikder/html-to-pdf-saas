using FluentValidation;

namespace HTPDF.Features.Auth.RefreshTokens;

public class RefreshTokenValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenValidator()
    {
        RuleFor(x => x.AccessToken)
            .NotEmpty()
            .WithMessage("ACCESS TOKEN IS REQUIRED");

        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("REFRESH TOKEN IS REQUIRED");
    }
}
