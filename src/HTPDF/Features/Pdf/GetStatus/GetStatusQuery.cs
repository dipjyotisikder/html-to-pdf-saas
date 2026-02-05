using HTPDF.Infrastructure.Common;
using MediatR;

namespace HTPDF.Features.Pdf.GetStatus;

public record GetStatusQuery(
    string JobId,
    string UserId
) : IRequest<Result<GetStatusResult>>;


public record GetStatusResult(
    string JobId,
    string Status,
    string Message,
    DateTime CreatedAt,
    DateTime? CompletedAt
);
