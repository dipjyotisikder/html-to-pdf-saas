namespace HTPDF.Infrastructure.Storage;

public interface IFileStorage
{
    Task<string> SaveAsync(byte[] data, string filename, CancellationToken cancellationToken = default);
    Task<byte[]?> ReadAsync(string filePath, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string filePath, CancellationToken cancellationToken = default);
    Task<int> DeleteExpiredAsync(CancellationToken cancellationToken = default);
    bool Exists(string filePath);
}
