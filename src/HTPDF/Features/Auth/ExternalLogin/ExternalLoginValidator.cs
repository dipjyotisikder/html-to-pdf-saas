using FluentValidation;
using HTPDF.Infrastructure.Database.Entities;
using Microsoft.AspNetCore.Identity;

namespace HTPDF.Features.Auth.ExternalLogin;

public class ExternalLoginValidator : AbstractValidator<ExternalLoginCommand>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ExternalLoginValidator(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email Is Required")
            .EmailAddress().WithMessage("Invalid Email Address")
            .MustAsync(BeActiveUser).WithMessage("Account Is Inactive. Please Contact Support.");

        RuleFor(x => x.Provider)
            .NotEmpty().WithMessage("Provider Is Required");

        RuleFor(x => x.ExternalId)
            .NotEmpty().WithMessage("External ID Is Required");
    }

    private async Task<bool> BeActiveUser(string email, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user == null || user.IsActive;
    }
}

