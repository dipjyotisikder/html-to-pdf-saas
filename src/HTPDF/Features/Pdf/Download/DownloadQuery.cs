using HTPDF.Infrastructure.Common;
using MediatR;

namespace HTPDF.Features.Pdf.Download;

public record DownloadQuery(
    string JobId,
    string UserId
) : IRequest<Result<DownloadResult>>;


public record DownloadResult(
    byte[] PdfBytes,
    string Filename
);
