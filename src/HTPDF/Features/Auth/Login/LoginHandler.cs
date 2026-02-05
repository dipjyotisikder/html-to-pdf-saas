using HTPDF.Features.Auth.Register;
using HTPDF.Infrastructure.Database;
using HTPDF.Infrastructure.Database.Entities;
using HTPDF.Infrastructure.Logging;
using MediatR;

using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace HTPDF.Features.Auth.Login;

public class LoginHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILoggingService<LoginHandler> _logger;

    public LoginHandler(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext context,
        IConfiguration configuration,
        ILoggingService<LoginHandler> logger)

    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return new LoginResult(false, "Invalid Email Or Password", null);
        }

        if (!user.IsActive)
        {
            return new LoginResult(false, "Account Is Inactive. Please Contact Support.", null);
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            return new LoginResult(false, "Account Is Locked Out. Please Try Again Later.", null);
        }

        if (!result.Succeeded)
        {
            return new LoginResult(false, "Invalid Email Or Password", null);
        }

        _logger.LogInfo(LogMessages.Auth.LoginSuccess, request.Email);

        var tokens = await GenerateTokensAsync(user);


        return new LoginResult(true, "Login Successful", tokens);
    }

    private async Task<AuthTokens> GenerateTokensAsync(ApplicationUser user)
    {
        var jwtSecret = _configuration["JwtSettings:SecretKey"]!;
        var issuer = _configuration["JwtSettings:Issuer"]!;
        var audience = _configuration["JwtSettings:Audience"]!;
        var expirationMinutes = int.Parse(_configuration["JwtSettings:AccessTokenExpirationMinutes"]!);
        var refreshExpirationDays = int.Parse(_configuration["JwtSettings:RefreshTokenExpirationDays"]!);

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

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = GenerateRefreshToken();

        var refreshTokenEntity = new Infrastructure.Database.Entities.RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            JwtId = jwtId,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshExpirationDays)
        };

        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        return new AuthTokens(
            accessToken,
            refreshToken,
            expirationMinutes * 60,
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
