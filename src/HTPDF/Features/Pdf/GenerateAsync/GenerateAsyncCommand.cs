using HTPDF.Infrastructure.Common;
using MediatR;

namespace HTPDF.Features.Pdf.GenerateAsync;

public record GenerateAsyncCommand(
    string UserId,
    string UserEmail,
    string HtmlContent,
    string? Filename,
    string Orientation,
    string PaperSize
) : IRequest<Result<GenerateAsyncResult>>;


public record GenerateAsyncResult(
    string JobId,
    string Status,
    string Message,
    DateTime CreatedAt
);

public record GenerateAsyncRequest(
    string HtmlContent,
    string? Filename,
    string? Orientation,
    string? PaperSize
);
