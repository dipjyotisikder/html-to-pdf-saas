using FluentValidation;
using HTPDF.Infrastructure.Common;
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

namespace HTPDF.Features.Auth.Register;

public class RegisterHandler : IRequestHandler<RegisterCommand, Result<AuthTokens>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtSettings _jwtSettings;
    private readonly IValidator<RegisterCommand> _validator;
    private readonly ILoggingService<RegisterHandler> _logger;

    public RegisterHandler(
        UserManager<ApplicationUser> userManager,
        IOptions<JwtSettings> options,
        IValidator<RegisterCommand> validator,
        ILoggingService<RegisterHandler> logger)

    {
        _userManager = userManager;
        _jwtSettings = options.Value;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<AuthTokens>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<AuthTokens>.Failure(validationResult.Errors.First().ErrorMessage);
        }

        var user = new ApplicationUser

        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return Result<AuthTokens>.Failure("User registration failed. Please ensure your password meets the requirements.");
        }

        await _userManager.AddToRoleAsync(user, "User");

        _logger.LogInfo(LogMessages.Auth.RegisterSuccess, request.Email);

        var tokens = await GenerateTokensAsync(user);


        return Result<AuthTokens>.Success(tokens, "Registration Successful");
    }


    private async Task<AuthTokens> GenerateTokensAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
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

