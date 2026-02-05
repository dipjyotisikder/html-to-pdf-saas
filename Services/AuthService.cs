using HTPDF.Configuration;
using HTPDF.Data;
using HTPDF.Data.Entities;
using HTPDF.Models.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace HTPDF.Services;

/// <summary>
/// Implementation of authentication service.
/// </summary>
public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _context;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext context,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<(bool Success, string Message, AuthResponse? Response)> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return (false, "User with this email already exists", null);
            }

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                EmailConfirmed = true // Auto-confirm for simplicity; implement email confirmation in production
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return (false, $"Registration failed: {errors}", null);
            }

            // Assign default role
            await _userManager.AddToRoleAsync(user, "User");

            _logger.LogInformation("User {Email} registered successfully", request.Email);

            // Generate tokens
            var authResponse = await GenerateTokensAsync(user);

            return (true, "Registration successful", authResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for {Email}", request.Email);
            return (false, "An error occurred during registration", null);
        }
    }

    /// <inheritdoc />
    public async Task<(bool Success, string Message, AuthResponse? Response)> LoginAsync(LoginRequest request)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return (false, "Invalid email or password", null);
            }

            if (!user.IsActive)
            {
                return (false, "Account is inactive. Please contact support.", null);
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

            if (result.IsLockedOut)
            {
                return (false, "Account is locked out. Please try again later.", null);
            }

            if (!result.Succeeded)
            {
                return (false, "Invalid email or password", null);
            }

            _logger.LogInformation("User {Email} logged in successfully", request.Email);

            var authResponse = await GenerateTokensAsync(user);

            return (true, "Login successful", authResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Email}", request.Email);
            return (false, "An error occurred during login", null);
        }
    }

    /// <inheritdoc />
    public async Task<(bool Success, string Message, AuthResponse? Response)> RefreshTokenAsync(RefreshTokenRequest request)
    {
        try
        {
            var principal = GetPrincipalFromExpiredToken(request.AccessToken);
            if (principal == null)
            {
                return (false, "Invalid access token", null);
            }

            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return (false, "Invalid token claims", null);
            }

            var storedToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && rt.UserId == userId);

            if (storedToken == null)
            {
                return (false, "Invalid refresh token", null);
            }

            if (storedToken.IsUsed)
            {
                return (false, "Refresh token has already been used", null);
            }

            if (storedToken.IsRevoked)
            {
                return (false, "Refresh token has been revoked", null);
            }

            if (storedToken.ExpiresAt < DateTime.UtcNow)
            {
                return (false, "Refresh token has expired", null);
            }

            var jti = principal.FindFirstValue(JwtRegisteredClaimNames.Jti);
            if (storedToken.JwtId != jti)
            {
                return (false, "Token mismatch", null);
            }

            // Mark old token as used
            storedToken.IsUsed = true;
            await _context.SaveChangesAsync();

            // Generate new tokens
            var user = storedToken.User!;
            var authResponse = await GenerateTokensAsync(user);

            _logger.LogInformation("Refresh token used successfully for user {UserId}", userId);

            return (true, "Token refreshed successfully", authResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return (false, "An error occurred during token refresh", null);
        }
    }

    /// <inheritdoc />
    public async Task<bool> RevokeTokenAsync(string refreshToken)
    {
        try
        {
            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (storedToken == null)
            {
                return false;
            }

            storedToken.IsRevoked = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Refresh token revoked for user {UserId}", storedToken.UserId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking token");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<(bool Success, string Message, AuthResponse? Response)> ExternalLoginAsync(ExternalLoginRequest request)
    {
        // This is a simplified implementation
        // In production, you would validate the ID token with the OAuth provider
        // For now, this is a placeholder

        try
        {
            // TODO: Validate ID token with the provider (Google, Microsoft, etc.)
            // Extract email and other claims from the validated token

            // For demonstration, returning not implemented
            _logger.LogWarning("External login attempted but not fully implemented");
            return (false, "External login is not fully implemented yet", null);

            // Production implementation would look like:
            /*
            var payload = await ValidateIdTokenAsync(request.Provider, request.IdToken);
            var email = payload.Email;
            
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Create new user from OAuth data
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FirstName = payload.FirstName,
                    LastName = payload.LastName
                };
                
                await _userManager.CreateAsync(user);
                await _userManager.AddToRoleAsync(user, "User");
            }
            
            var authResponse = await GenerateTokensAsync(user);
            return (true, "External login successful", authResponse);
            */
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during external login");
            return (false, "An error occurred during external login", null);
        }
    }

    private async Task<AuthResponse> GenerateTokensAsync(ApplicationUser user)
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

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var accessToken = tokenHandler.WriteToken(token);

        // Generate refresh token
        var refreshToken = GenerateRefreshToken();
        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            JwtId = claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays)
        };

        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = _jwtSettings.AccessTokenExpirationMinutes * 60,
            Email = user.Email,
            Roles = roles.ToList()
        };
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = _jwtSettings.Audience,
            ValidateIssuer = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)),
            ValidateLifetime = false // We don't validate lifetime here since token might be expired
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
}
