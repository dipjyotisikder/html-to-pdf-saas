using FluentValidation;
using HTPDF.Infrastructure.Common;
using HTPDF.Infrastructure.Database;
using HTPDF.Infrastructure.Database.Entities;
using HTPDF.Infrastructure.Storage;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HTPDF.Features.Pdf.Download;

public class DownloadHandler : IRequestHandler<DownloadQuery, Result<DownloadResult>>
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

    public async Task<Result<DownloadResult>> Handle(DownloadQuery request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<DownloadResult>.Failure(validationResult.Errors.First().ErrorMessage);
        }

        var job = await _context.PdfJobs
            .FirstAsync(j => j.JobId == request.JobId && j.UserId == request.UserId && j.Status == JobStatus.Completed, cancellationToken);

        if (string.IsNullOrEmpty(job.FilePath))
        {
            return Result<DownloadResult>.Failure("File path not found for the completed job.");
        }

        var pdfBytes = await _fileStorage.ReadAsync(job.FilePath, cancellationToken);

        if (pdfBytes == null)
        {
            return Result<DownloadResult>.Failure("The PDF file could not be read from storage.");
        }

        var result = new DownloadResult(pdfBytes, job.Filename);
        return Result<DownloadResult>.Success(result);
    }
}

