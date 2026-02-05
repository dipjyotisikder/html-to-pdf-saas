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

namespace HTPDF.Features.Auth.Login;

public class LoginHandler : IRequestHandler<LoginCommand, Result<AuthTokens>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _context;
    private readonly JwtSettings _jwtSettings;
    private readonly IValidator<LoginCommand> _validator;
    private readonly ILoggingService<LoginHandler> _logger;

    public LoginHandler(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext context,
        IOptions<JwtSettings> options,
        IValidator<LoginCommand> validator,
        ILoggingService<LoginHandler> logger)

    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _jwtSettings = options.Value;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<AuthTokens>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<AuthTokens>.Failure(validationResult.Errors.First().ErrorMessage);
        }

        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user == null)
        {
            return Result<AuthTokens>.Failure("Invalid Email Or Password");
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);


        if (result.IsLockedOut)
        {
            return Result<AuthTokens>.Failure("Account is temporarily locked due to multiple failed attempts. Please try again later.");
        }

        if (!result.Succeeded)
        {
            return Result<AuthTokens>.Failure("Invalid Email Or Password");
        }

        _logger.LogInfo(LogMessages.Auth.LoginSuccess, request.Email);

        var tokens = await GenerateTokensAsync(user);


        return Result<AuthTokens>.Success(tokens, "Login Successful");
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
