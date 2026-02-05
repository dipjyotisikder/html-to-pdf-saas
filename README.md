# HTML to PDF Service

A production-ready ASP.NET Core 8.0 web API for asynchronous PDF generation from HTML content with authentication, background job processing, and robust error handling.

## Overview

This service provides a RESTful API for converting HTML content to PDF documents with enterprise features including user authentication, rate limiting, job queuing, and email notifications. Built using Clean Architecture principles with CQRS pattern via MediatR.

## Features

- **Asynchronous PDF Generation**: Convert HTML to PDF using DinkToPdf with background job processing
- **Authentication & Authorization**: JWT-based authentication with support for Google and Microsoft OAuth providers
- **Background Job Processing**: Reliable job queue with retry logic using Polly for resilience
- **Rate Limiting**: Configurable request throttling to prevent abuse
- **Email Notifications**: Automated email notifications on job completion
- **File Management**: Automatic cleanup of generated PDFs with configurable retention
- **Health Checks**: Endpoint for monitoring service health
- **API Documentation**: Interactive Swagger/OpenAPI documentation
- **HTML Sanitization**: Security layer to prevent XSS attacks in HTML content
- **Database Persistence**: Entity Framework Core with SQL Server for job tracking and user management

## Technology Stack

- .NET 8.0
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- DinkToPdf & PdfSharpCore for PDF generation
- MediatR for CQRS pattern
- FluentValidation for request validation
- JWT Bearer authentication
- MailKit for email notifications
- AspNetCoreRateLimit for throttling
- Polly for resilience and retry policies
- Docker support

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- SQL Server (LocalDB, Express, or full installation)
- Visual Studio 2022 / VS Code / Rider (optional)

### Installation

1. Clone the repository:
```bash
git clone https://github.com/yourusername/html-to-pdf-dotnet.git
cd html-to-pdf-dotnet
```

2. Restore dependencies:
```bash
dotnet restore
```

3. Configure application settings:

Copy the example configuration file:
```bash
cp appsettings.example.json src/HTPDF/appsettings.json
```

Then update `src/HTPDF/appsettings.json` with your specific values.

**Important:** Never commit sensitive credentials to version control. Use User Secrets for development and environment variables for production.

**Recommended: Use User Secrets for development:**
```bash
cd src/HTPDF
dotnet user-secrets init
dotnet user-secrets set "JwtSettings:SecretKey" "your-secret-key-at-least-32-characters"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\\mssqllocaldb;Database=HtpdfDb;Trusted_Connection=True;"
dotnet user-secrets set "EmailSettings:Username" "your-email@gmail.com"
dotnet user-secrets set "EmailSettings:Password" "your-smtp-password"
```

4. Apply database migrations:
```bash
dotnet ef database update --project src/HTPDF
```

5. Run the application:
```bash
dotnet run --project src/HTPDF
```

The API will be available at `https://localhost:7000` (HTTPS) or `http://localhost:5000` (HTTP).

## API Usage

### Authentication

Register a new user:
```bash
POST /auth/register
{
  "email": "user@example.com",
  "password": "SecurePassword123",
  "firstName": "John",
  "lastName": "Doe"
}
```

Login:
```bash
POST /auth/login
{
  "email": "user@example.com",
  "password": "SecurePassword123"
}
```

### PDF Generation

Generate PDF asynchronously (requires authentication):
```bash
POST /pdf/generate/async
Authorization: Bearer {your-jwt-token}
{
  "htmlContent": "<html><body><h1>Hello World</h1></body></html>",
  "filename": "my-document.pdf",
  "orientation": "Portrait",
  "paperSize": "A4"
}
```

Check job status:
```bash
GET /pdf/jobs/{jobId}
Authorization: Bearer {your-jwt-token}
```

Download generated PDF:
```bash
GET /pdf/download/{jobId}
Authorization: Bearer {your-jwt-token}
```

## Project Structure

```
html-to-pdf-dotnet/
├── src/
│   └── HTPDF/
│       ├── Configuration/          # Configuration models
│       ├── Controllers/            # API controllers
│       ├── Features/               # Feature-based organization
│       │   ├── Auth/              # Authentication features
│       │   └── Pdf/               # PDF generation features
│       ├── Infrastructure/         # Infrastructure concerns
│       │   ├── BackgroundJobs/    # Background job processors
│       │   ├── Behaviors/         # MediatR behaviors
│       │   ├── Database/          # EF Core DbContext and entities
│       │   ├── Email/             # Email services
│       │   ├── Middleware/        # Custom middleware
│       │   └── Storage/           # File storage services
│       ├── Middleware/            # API middleware
│       ├── Models/                # Request/Response models
│       └── Services/              # Application services
├── Directory.Build.props          # Common build properties
├── Directory.Packages.props       # Central package management
└── HTPDF.slnx                     # Solution file (XML format)
```

## Configuration

All sensitive configuration should be managed through:
- **User Secrets** for local development
- **Environment Variables** for production deployments
- **Azure Key Vault** or similar secret management for cloud deployments

See [appsettings.example.json](appsettings.example.json) for a complete configuration template.

### Required Settings

| Setting | Description | Example |
|---------|-------------|---------|
| `ConnectionStrings:DefaultConnection` | SQL Server connection string | `Server=(localdb)\\mssqllocaldb;Database=HtpdfDb;Trusted_Connection=True;` |
| `JwtSettings:SecretKey` | JWT signing key (min 32 chars) | Use `dotnet user-secrets` |
| `JwtSettings:Issuer` | JWT issuer URL | `https://yourdomain.com` |
| `JwtSettings:Audience` | JWT audience URL | `https://yourdomain.com` |

### Optional Settings

**Email Notifications:**
```bash
dotnet user-secrets set "EmailSettings:SmtpHost" "smtp.gmail.com"
dotnet user-secrets set "EmailSettings:SmtpPort" "587"
dotnet user-secrets set "EmailSettings:Username" "your-email@gmail.com"
dotnet user-secrets set "EmailSettings:Password" "your-app-password"
```

**OAuth Providers:**
```bash
dotnet user-secrets set "Authentication:Google:ClientId" "your-client-id"
dotnet user-secrets set "Authentication:Google:ClientSecret" "your-client-secret"
```

### Rate Limiting

Configure in `appsettings.json`:
```json
{
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": false,
    "GeneralRules": [
      {
        "Endpoint": "*",
        "Period": "1m",
        "Limit": 60
      }
    ]
  }
}
```

### Email Settings

Configure SMTP settings for notifications:
```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderEmail": "noreply@example.com",
    "SenderName": "PDF Service"
  }
}
```

## Security Best Practices

**Never commit secrets to version control:**
- Use .NET User Secrets for local development
- Use environment variables or secret managers in production
- The `appsettings.json` file should contain only non-sensitive defaults
- Review `.gitignore` to ensure sensitive files are excluded

**Included in `.gitignore`:**
- `appsettings.Local.json`
- `appsettings.*.Local.json`
- `*.db` and database files
- User secrets and `.env` files

**For production deployments:**
- Use HTTPS only
- Store secrets in Azure Key Vault, AWS Secrets Manager, or similar
- Enable authentication and authorization
- Configure appropriate rate limits
- Regularly update dependencies for security patches

## Docker Support

Build and run using Docker:
```bash
docker build -t htpdf-service .
docker run -p 5000:8080 htpdf-service
```

## Contributing

Contributions are welcome. Please follow these guidelines:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.

## Acknowledgments

- DinkToPdf for PDF generation capabilities
- MediatR for implementing CQRS pattern
- ASP.NET Core team for the excellent framework

