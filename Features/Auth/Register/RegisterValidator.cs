using FluentValidation;

namespace HTPDF.Features.Auth.Register;

public class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email Is Required")
            .EmailAddress().WithMessage("Invalid Email Address");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password Is Required")
            .MinimumLength(6).WithMessage("Password Must Be At Least 6 Characters");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Passwords Do Not Match");

        RuleFor(x => x.FirstName)
            .MaximumLength(100).WithMessage("First Name Cannot Exceed 100 Characters");

        RuleFor(x => x.LastName)
            .MaximumLength(100).WithMessage("Last Name Cannot Exceed 100 Characters");
    }
}
