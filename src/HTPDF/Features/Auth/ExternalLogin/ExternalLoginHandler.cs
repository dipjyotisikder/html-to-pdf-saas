using FluentValidation;
using HTPDF.Features.Auth.Register;
using HTPDF.Infrastructure.Common;
using HTPDF.Infrastructure.Database;
using HTPDF.Infrastructure.Database.Entities;
using HTPDF.Infrastructure.Logging;
using HTPDF.Infrastructure.Settings;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace HTPDF.Features.Auth.ExternalLogin;

public class ExternalLoginHandler : IRequestHandler<ExternalLoginCommand, Result<AuthTokens>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly JwtSettings _jwtSettings;
    private readonly IValidator<ExternalLoginCommand> _validator;
    private readonly ILoggingService<ExternalLoginHandler> _logger;

    public ExternalLoginHandler(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IOptions<JwtSettings> options,
        IValidator<ExternalLoginCommand> validator,
        ILoggingService<ExternalLoginHandler> logger)

    {
        _userManager = userManager;
        _context = context;
        _jwtSettings = options.Value;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<AuthTokens>> Handle(ExternalLoginCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<AuthTokens>.Failure(validationResult.Errors.First().ErrorMessage);
        }

        var user = await _userManager.FindByEmailAsync(request.Email);


        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName ?? "User",
                LastName = request.LastName ?? "",
                EmailConfirmed = true,
                IsActive = true
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                return Result<AuthTokens>.Failure("Failed to create user account from external provider.");
            }

            await _userManager.AddToRoleAsync(user, "User");

            _logger.LogInfo(LogMessages.Auth.NewUserCreatedViaProvider, request.Provider, request.Email);
        }

        var loginInfo = new UserLoginInfo(request.Provider, request.ExternalId, request.Provider);

        var existingLogin = await _userManager.FindByLoginAsync(loginInfo.LoginProvider, loginInfo.ProviderKey);

        if (existingLogin == null)
        {
            var addLoginResult = await _userManager.AddLoginAsync(user, loginInfo);
            if (!addLoginResult.Succeeded)
            {
                return Result<AuthTokens>.Failure("Failed to link external authentication provider.");
            }
        }

        _logger.LogInfo(LogMessages.Auth.ExternalLoginSuccess, request.Email, request.Provider);

        var tokens = await GenerateTokensAsync(user);


        return Result<AuthTokens>.Success(tokens, "External Login Successful");
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
