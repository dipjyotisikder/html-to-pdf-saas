using FluentValidation;
using HTPDF.Features.Auth.Register;
using HTPDF.Infrastructure.Database;
using HTPDF.Infrastructure.Database.Entities;
using HTPDF.Infrastructure.Logging;
using HTPDF.Infrastructure.Settings;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace HTPDF.Features.Auth.RefreshTokens;

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResult>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly JwtSettings _jwtSettings;
    private readonly IValidator<RefreshTokenCommand> _validator;
    private readonly ILoggingService<RefreshTokenHandler> _logger;

    public RefreshTokenHandler(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IOptions<JwtSettings> options,
        IValidator<RefreshTokenCommand> validator,
        ILoggingService<RefreshTokenHandler> logger)

    {
        _userManager = userManager;
        _context = context;
        _jwtSettings = options.Value;
        _validator = validator;
        _logger = logger;
    }

    public async Task<RefreshTokenResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return new RefreshTokenResult(false, validationResult.Errors.First().ErrorMessage, null);
        }

        var principal = GetPrincipalFromExpiredToken(request.AccessToken, _jwtSettings.SecretKey, _jwtSettings.Issuer, _jwtSettings.Audience);


        if (principal == null)
        {
            return new RefreshTokenResult(false, "Invalid Access Token", null);
        }

        var jwtId = principal.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti)?.Value;
        var userId = principal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(jwtId) || string.IsNullOrEmpty(userId))
        {
            return new RefreshTokenResult(false, "Invalid Token Claims", null);
        }

        var storedRefreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == request.RefreshToken && x.JwtId == jwtId, cancellationToken);

        if (storedRefreshToken == null)
        {
            return new RefreshTokenResult(false, "Refresh Token Does Not Exist", null);
        }

        if (storedRefreshToken.IsUsed)
        {
            return new RefreshTokenResult(false, "Refresh Token Already Used", null);
        }

        if (storedRefreshToken.IsRevoked)
        {
            return new RefreshTokenResult(false, "Refresh Token Has Been Revoked", null);
        }

        if (storedRefreshToken.ExpiresAt < DateTime.UtcNow)
        {
            return new RefreshTokenResult(false, "Refresh Token Has Expired", null);
        }

        storedRefreshToken.IsUsed = true;
        await _context.SaveChangesAsync(cancellationToken);

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive)
        {
            return new RefreshTokenResult(false, "User Not Found Or Inactive", null);
        }

        _logger.LogInfo(LogMessages.Auth.RefreshTokenUsed, user.Email);

        var tokens = await GenerateTokensAsync(user);


        return new RefreshTokenResult(true, "Token Refreshed Successfully", tokens);
    }

    private static ClaimsPrincipal? GetPrincipalFromExpiredToken(string token, string secret, string issuer, string audience)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }

    private async Task<AuthTokens> GenerateTokensAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var jwtId = Guid.NewGuid().ToString();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, jwtId),
            new(JwtRegisteredClaimNames.Sub, user.Email!)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            signingCredentials: credentials
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            JwtId = jwtId,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays)
        };


        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        return new AuthTokens(
            accessToken,
            refreshToken,
            _jwtSettings.AccessTokenExpirationMinutes * 60,
            user.Email!,
            roles.ToList()
        );

    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}
