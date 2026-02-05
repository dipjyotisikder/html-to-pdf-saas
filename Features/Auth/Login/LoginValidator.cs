using FluentValidation;

namespace HTPDF.Features.Auth.Login;

public class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email Is Required")
            .EmailAddress().WithMessage("Invalid Email Address");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password Is Required");
    }
}
