using FluentValidation;
using HTPDF.Infrastructure.Database;
using HTPDF.Infrastructure.Database.Entities;
using HTPDF.Infrastructure.Storage;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HTPDF.Features.Pdf.Download;

public class DownloadHandler : IRequestHandler<DownloadQuery, DownloadResult?>
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorage _fileStorage;
    private readonly IValidator<DownloadQuery> _validator;

    public DownloadHandler(ApplicationDbContext context, IFileStorage fileStorage, IValidator<DownloadQuery> validator)
    {
        _context = context;
        _fileStorage = fileStorage;
        _validator = validator;
    }

    public async Task<DownloadResult?> Handle(DownloadQuery request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var job = await _context.PdfJobs
            .FirstAsync(j => j.JobId == request.JobId && j.UserId == request.UserId && j.Status == JobStatus.Completed, cancellationToken);


        var pdfBytes = await _fileStorage.ReadAsync(job.FilePath, cancellationToken);

        if (pdfBytes == null)
        {
            return null;
        }

        return new DownloadResult(pdfBytes, job.Filename);
    }
}
