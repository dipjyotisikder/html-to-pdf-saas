namespace HTPDF.Infrastructure.Logging;

public static class LogMessages
{
    public static class Auth
    {
        public const string ExternalLoginSuccess = "User {Email} Logged In Via {Provider}";
        public const string NewUserCreatedViaProvider = "New User Created Via {Provider}: {Email}";
        public const string LoginSuccess = "User {Email} Logged In Successfully";
        public const string RefreshTokenUsed = "Refresh Token Used By User {Email}";
        public const string RegisterSuccess = "User {Email} Registered Successfully";
    }

    public static class Pdf
    {
        public const string JobCreated = "Job {JobId} Submitted For User {UserId}";
        public const string JobDeleted = "Job {JobId} Deleted By User {UserId}";
        public const string FileDeleted = "Deleted PDF File {FilePath} For Job {JobId}";
        public const string FileDeleteFailed = "Failed To Delete PDF File {FilePath} For Job {JobId}";
        public const string DashboardLoaded = "Dashboard Loaded For User {UserId} - {TotalJobs} Total Jobs";
        public const string UserJobsRetrieved = "Retrieved {Count} Jobs For User {UserId}";
    }

    public static class Infrastructure
    {
        public const string ApiStarted = "HTML To PDF API v3.0 Started - Vertical Slice Architecture";
        public const string SwaggerUrl = "Swagger UI: /swagger";
        public const string FileCleanupError = "Error During File Cleanup";
        public const string FileCleanupServiceStarted = "File Cleanup Service Started. Running Every {Interval} Hours";
        public const string FileCleanupStarted = "Starting File Cleanup";
        public const string FileCleanupCompleted = "File Cleanup Completed. Deleted {Count} Files";

        public const string OutboxProcessorStarted = "Outbox Processor Started. Processing Every {Interval} Seconds";
        public const string OutboxProcessingError = "Error Processing Outbox Messages";
        public const string ProcessingOutboxCount = "Processing {Count} Outbox Messages";
        public const string ProcessingOutboxMessage = "Processing Outbox Message {MessageId}, Type: {Type}, Attempt: {Attempt}";
        public const string OutboxMessageProcessed = "Outbox Message {MessageId} Processed Successfully";
        public const string OutboxMessageProcessingError = "Error Processing Outbox Message {MessageId}";
        public const string OutboxMessagePermanentlyFailed = "Outbox Message {MessageId} Permanently Failed After {Attempts} Attempts";
        public const string OutboxMessageRetry = "Outbox Message {MessageId} Failed, Will Retry (Attempt {Attempt}/{Max}). Next Retry At {NextRetry}";

        public const string PdfJobProcessorStarted = "PDF Job Processor Started";
        public const string ProcessingPdfJob = "Processing PDF Job {JobId}, Attempt {Attempt}";
        public const string PdfJobCompleted = "Job {JobId} Completed Successfully";
        public const string PdfJobFailed = "Job {JobId} Failed";

        public const string EmailSendRetry = "Email Send Attempt {RetryCount} Failed. Waiting {TimeSpan} Before Next Retry";
        public const string EmailSentSuccess = "Email Sent Successfully To {ToEmail}";
        public const string EmailSendFailed = "Failed To Send Email To {ToEmail} After Retries";

        public const string StorageDirectoryCreated = "Created Storage Directory: {Path}";
        public const string PdfSaved = "PDF Saved Successfully: {FilePath}, Size: {Size} Bytes";
        public const string PdfNotFound = "PDF File Not Found: {FilePath}";
        public const string PdfNotFoundForDeletion = "PDF File Not Found For Deletion: {FilePath}";
        public const string PdfDeleted = "PDF File Deleted: {FilePath}";
        public const string ExpiredPdfsDeleted = "Deleted {Count} Expired PDF Files";

        public const string ValidationError = "Validation Error";
        public const string UnauthorizedAccess = "Unauthorized Access";
        public const string UnhandledException = "Unhandled Exception";

        public const string AdminUserCreated = "Admin User Created: {Email}";
        public const string DatabaseMigrationError = "Error During Database Migration Or Seeding";
    }
}
