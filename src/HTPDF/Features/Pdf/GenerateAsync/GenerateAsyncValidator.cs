using FluentValidation;

namespace HTPDF.Features.Pdf.GenerateAsync;

public class GenerateAsyncValidator : AbstractValidator<GenerateAsyncCommand>
{
    public GenerateAsyncValidator()
    {
        RuleFor(x => x.HtmlContent)
            .NotEmpty().WithMessage("HTML Content Is Required")
            .MaximumLength(2097152).WithMessage("HTML Content Cannot Exceed 2MB");

        RuleFor(x => x.Filename)
            .MaximumLength(255).WithMessage("Filename Cannot Exceed 255 Characters");

        RuleFor(x => x.Orientation)
            .Must(x => x == "Portrait" || x == "Landscape")
            .WithMessage("Orientation Must Be Either Portrait Or Landscape");

        RuleFor(x => x.PaperSize)
            .Must(x => new[] { "A4", "A3", "LETTER", "LEGAL" }.Contains(x.ToUpperInvariant()))
            .WithMessage("Paper Size Must Be A4, A3, Letter, Or Legal");
    }
}
