using MediatR;

namespace HTPDF.Features.Pdf.DeleteJob;

public record DeleteJobCommand(
    string JobId,
    string UserId
) : IRequest<DeleteJobResult>;

public record DeleteJobResult(
    bool Success,
    string Message
);
