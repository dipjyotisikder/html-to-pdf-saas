using System;

namespace HTPDF.Infrastructure.Logging;

public interface ILoggingService<T>
{
    void LogInfo(string message, params object?[] args);
    void LogWarning(string message, params object?[] args);
    void LogWarning(Exception ex, string message, params object?[] args);
    void LogError(string message, params object?[] args);
    void LogError(Exception ex, string message, params object?[] args);
    void LogDebug(string message, params object?[] args);
}
