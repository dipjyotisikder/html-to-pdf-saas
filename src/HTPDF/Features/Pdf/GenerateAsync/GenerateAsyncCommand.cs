using MediatR;

namespace HTPDF.Features.Pdf.GenerateAsync;

public record GenerateAsyncCommand(
    string UserId,
    string UserEmail,
    string HtmlContent,
    string? Filename,
    string Orientation,
    string PaperSize
) : IRequest<GenerateAsyncResult>;

public record GenerateAsyncResult(
    string JobId,
    string Status,
    string Message,
    DateTime CreatedAt
);
