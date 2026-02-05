using FluentValidation;
using HTPDF.Infrastructure.Database;
using HTPDF.Infrastructure.Logging;
using HTPDF.Infrastructure.Storage;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HTPDF.Features.Pdf.DeleteJob;

public class DeleteJobHandler : IRequestHandler<DeleteJobCommand, DeleteJobResult>
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorage _fileStorage;
    private readonly IValidator<DeleteJobCommand> _validator;
    private readonly ILoggingService<DeleteJobHandler> _logger;

    public DeleteJobHandler(
        ApplicationDbContext context,
        IFileStorage fileStorage,
        IValidator<DeleteJobCommand> validator,
        ILoggingService<DeleteJobHandler> logger)

    {
        _context = context;
        _fileStorage = fileStorage;
        _validator = validator;
        _logger = logger;
    }

    public async Task<DeleteJobResult> Handle(DeleteJobCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return new DeleteJobResult(false, validationResult.Errors.First().ErrorMessage);
        }

        var job = await _context.PdfJobs
            .FirstAsync(j => j.JobId == request.JobId && j.UserId == request.UserId, cancellationToken);


        if (!string.IsNullOrEmpty(job.FilePath))
        {
            try
            {
                await _fileStorage.DeleteAsync(job.FilePath, cancellationToken);
                _logger.LogInfo(LogMessages.Pdf.FileDeleted, job.FilePath, job.JobId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, LogMessages.Pdf.FileDeleteFailed, job.FilePath, job.JobId);
            }
        }

        _context.PdfJobs.Remove(job);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInfo(LogMessages.Pdf.JobDeleted, request.JobId, request.UserId);


        return new DeleteJobResult(true, "Job Deleted Successfully");
    }
}
