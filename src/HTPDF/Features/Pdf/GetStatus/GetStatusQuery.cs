using MediatR;

namespace HTPDF.Features.Pdf.GetStatus;

public record GetStatusQuery(
    string JobId,
    string UserId
) : IRequest<GetStatusResult?>;

public record GetStatusResult(
    string JobId,
    string Status,
    string Message,
    DateTime CreatedAt,
    DateTime? CompletedAt
);
