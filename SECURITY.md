# Security Policy

## Reporting Security Vulnerabilities

If you discover a security vulnerability in this project, please report it by emailing the maintainers. Please do not create public GitHub issues for security vulnerabilities.

## Secure Configuration

### Development Environment

Use .NET User Secrets to store sensitive configuration:

```bash
cd src/HTPDF
dotnet user-secrets init
dotnet user-secrets set "JwtSettings:SecretKey" "your-secret-key"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your-connection-string"
dotnet user-secrets set "EmailSettings:Password" "your-password"
```

### Production Environment

**Never** store secrets in configuration files. Use one of the following approaches:

1. **Environment Variables**
   ```bash
   export JwtSettings__SecretKey="your-secret-key"
   export ConnectionStrings__DefaultConnection="your-connection-string"
   ```

2. **Azure Key Vault** (recommended for Azure deployments)
3. **AWS Secrets Manager** (for AWS deployments)
4. **Docker Secrets** (for Docker Swarm)
5. **Kubernetes Secrets** (for Kubernetes)

### What NOT to Commit

Never commit the following to version control:
- Real API keys or secrets
- Database passwords
- JWT secret keys
- SMTP credentials
- OAuth client secrets
- Connection strings with embedded credentials
- Any `.Local.json` configuration files
- Database files (`.db`, `.mdf`, `.ldf`)

### Security Checklist

Before deploying to production:

- [ ] All secrets are stored in a secure secret manager
- [ ] HTTPS is enforced (HTTP is disabled)
- [ ] CORS is configured appropriately
- [ ] Rate limiting is enabled and configured
- [ ] Authentication is required for all sensitive endpoints
- [ ] Input validation is enabled (FluentValidation)
- [ ] HTML sanitization is active (prevents XSS)
- [ ] SQL injection protection (Entity Framework parameterized queries)
- [ ] File upload size limits are configured
- [ ] Background job processing has proper error handling
- [ ] Database connection uses least-privilege credentials
- [ ] Logging doesn't include sensitive data
- [ ] Dependencies are up to date

### Default Security Features

This application includes:

- **JWT Authentication**: Secure token-based authentication
- **HTML Sanitization**: XSS attack prevention using HtmlSanitizer
- **Rate Limiting**: Protection against abuse and DoS attacks
- **Input Validation**: FluentValidation for all requests
- **SQL Injection Protection**: Entity Framework Core with parameterized queries
- **CORS Configuration**: Configurable cross-origin policies
- **Password Hashing**: ASP.NET Core Identity with secure defaults
- **Request Size Limits**: Configurable limits on HTML content size

### Recommended JWT Secret Key

Generate a secure JWT secret key:

**Using PowerShell:**
```powershell
[Convert]::ToBase64String((1..64 | ForEach-Object { Get-Random -Maximum 256 }))
```

**Using OpenSSL:**
```bash
openssl rand -base64 64
```

**Using .NET:**
```bash
dotnet user-secrets set "JwtSettings:SecretKey" "$(openssl rand -base64 64)"
```

The key should be at least 256 bits (32 bytes) for HS256 algorithm.

### Additional Security Recommendations

1. **Enable HTTPS Redirection** - Already configured in the application
2. **Use Strong Passwords** - Configure password policy in Identity settings
3. **Enable Account Lockout** - Configured with 5 failed attempts
4. **Regular Security Updates** - Keep NuGet packages updated
5. **Audit Logging** - Consider adding audit logs for sensitive operations
6. **Database Encryption** - Use Transparent Data Encryption (TDE) for SQL Server
7. **Network Security** - Use firewalls and network isolation
8. **Monitoring** - Implement application monitoring and alerting

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| 1.x     | :white_check_mark: |

## Security Updates

Security updates will be released as soon as possible after a vulnerability is discovered and verified.
