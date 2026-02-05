using MediatR;

namespace HTPDF.Features.Pdf.Download;

public record DownloadQuery(
    string JobId,
    string UserId
) : IRequest<DownloadResult?>;

public record DownloadResult(
    byte[] PdfBytes,
    string Filename
);
