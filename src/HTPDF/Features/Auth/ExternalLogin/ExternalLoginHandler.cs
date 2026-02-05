using HTPDF.Features.Auth.Register;
using HTPDF.Infrastructure.Database;
using HTPDF.Infrastructure.Database.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace HTPDF.Features.Auth.ExternalLogin;

public class ExternalLoginHandler : IRequestHandler<ExternalLoginCommand, ExternalLoginResult>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ExternalLoginHandler> _logger;

    public ExternalLoginHandler(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<ExternalLoginHandler> logger)
    {
        _userManager = userManager;
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ExternalLoginResult> Handle(ExternalLoginCommand request, CancellationToken cancellationToken)
    {
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
                return new ExternalLoginResult(false, "Failed To Create User Account", null);
            }

            await _userManager.AddToRoleAsync(user, "User");

            _logger.LogInformation("New User Created Via {Provider}: {Email}", request.Provider, request.Email);
        }
        else if (!user.IsActive)
        {
            return new ExternalLoginResult(false, "Account Is Inactive. Please Contact Support.", null);
        }

        var loginInfo = new UserLoginInfo(request.Provider, request.ExternalId, request.Provider);
        var existingLogin = await _userManager.FindByLoginAsync(loginInfo.LoginProvider, loginInfo.ProviderKey);

        if (existingLogin == null)
        {
            var addLoginResult = await _userManager.AddLoginAsync(user, loginInfo);
            if (!addLoginResult.Succeeded)
            {
                return new ExternalLoginResult(false, "Failed To Link External Provider", null);
            }
        }

        _logger.LogInformation("User {Email} Logged In Via {Provider}", request.Email, request.Provider);

        var tokens = await GenerateTokensAsync(user);

        return new ExternalLoginResult(true, "External Login Successful", tokens);
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

        var refreshTokenEntity = new RefreshToken
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
