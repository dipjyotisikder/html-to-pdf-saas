using FluentValidation;

namespace HTPDF.Features.Pdf.GetUserJobs;

public class GetUserJobsValidator : AbstractValidator<GetUserJobsQuery>
{
    public GetUserJobsValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("Page Number Must Be At Least 1");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page Size Must Be Between 1 And 100");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId Is Required");
    }
}
