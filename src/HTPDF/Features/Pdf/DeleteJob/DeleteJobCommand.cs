using HTPDF.Infrastructure.Common;
using MediatR;

namespace HTPDF.Features.Pdf.DeleteJob;

public record DeleteJobCommand(
    string JobId,
    string UserId
) : IRequest<Result>;

