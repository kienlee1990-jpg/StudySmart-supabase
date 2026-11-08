using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuanLyTaiKhoanAPI.Data;
using QuanLyTaiKhoanAPI.DTOs;
using QuanLyTaiKhoanAPI.DTOs.Audit;
using QuanLyTaiKhoanAPI.DTOs.Auth;
using QuanLyTaiKhoanAPI.Entities;
using QuanLyTaiKhoanAPI.Helper;
using QuanLyTaiKhoanAPI.Interfaces;
using QuanLyTaiKhoanAPI.Middlewares;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace QuanLyTaiKhoanAPI.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _config;
    private readonly AppDbContext _context;
    private readonly ILogger<AuthService> _logger;
    private readonly IPermissionService _permissionService;
    private readonly IEmailSender _emailSender;
    private readonly IAuditLogService _auditLogService;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IConfiguration config,
        AppDbContext context,
        ILogger<AuthService> logger,
        IPermissionService permissionService,
        IEmailSender emailSender,
        IAuditLogService auditLogService)
    {
        _userManager = userManager;
        _config = config;
        _context = context;
        _logger = logger;
        _permissionService = permissionService;
        _emailSender = emailSender;
        _auditLogService = auditLogService;
    }

    private int AccessTokenExpirationHours =>
        int.Parse(_config["Jwt:AccessTokenExpirationHours"] ?? "3");

    private int RefreshTokenExpirationDays =>
        int.Parse(_config["Jwt:RefreshTokenExpirationDays"] ?? "7");

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var loginName = dto.Email?.Trim() ?? string.Empty;
        var user = await _userManager.FindByEmailAsync(loginName)
            ?? await _userManager.FindByNameAsync(loginName);

        if (user == null)
        {
            await _auditLogService.WriteAsync(new AuditLogEntry
            {
                Action = "LoginFailed",
                Succeeded = false,
                Details = $"LoginName={loginName}"
            });

            _logger.LogWarning("Login failed for {LoginName}", loginName);
            throw new Exception("INVALID_CREDENTIALS");
        }

        if (!user.IsActive)
        {
            await _auditLogService.WriteAsync(new AuditLogEntry
            {
                Action = "LoginBlocked",
                Succeeded = false,
                TargetUserId = user.Id,
                TargetUserName = user.UserName,
                Details = "Account inactive"
            });

            _logger.LogWarning("Login blocked for inactive account {Email}", user.Email);
            throw new Exception("ACCOUNT_INACTIVE");
        }

        if (!user.LockoutEnabled)
        {
            user.LockoutEnabled = true;
            await _userManager.UpdateAsync(user);
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            await _auditLogService.WriteAsync(new AuditLogEntry
            {
                Action = "LoginLockedOut",
                Succeeded = false,
                TargetUserId = user.Id,
                TargetUserName = user.UserName,
                Details = $"LockoutEnd={user.LockoutEnd:O}"
            });

            _logger.LogWarning("Login blocked for locked account {Email}", user.Email);
            throw new Exception("ACCOUNT_LOCKED");
        }

        if (!await _userManager.CheckPasswordAsync(user, dto.Password))
        {
            await _userManager.AccessFailedAsync(user);
            var failedCount = await _userManager.GetAccessFailedCountAsync(user);
            var isLockedOut = await _userManager.IsLockedOutAsync(user);

            await _auditLogService.WriteAsync(new AuditLogEntry
            {
                Action = isLockedOut ? "AccountLocked" : "LoginFailed",
                Succeeded = false,
                TargetUserId = user.Id,
                TargetUserName = user.UserName,
                Details = $"FailedAttempts={failedCount}"
            });

            _logger.LogWarning("Login failed for {LoginName}", loginName);
            throw new Exception(isLockedOut ? "ACCOUNT_LOCKED" : "INVALID_CREDENTIALS");
        }

        if (await _userManager.GetAccessFailedCountAsync(user) > 0)
        {
            await _userManager.ResetAccessFailedCountAsync(user);
        }

        await _auditLogService.WriteAsync(new AuditLogEntry
        {
            Action = "LoginSucceeded",
            ActorUserId = user.Id,
            ActorUserName = user.UserName,
            TargetUserId = user.Id,
            TargetUserName = user.UserName
        });

        _logger.LogInformation("User logged in: {Email}", user.Email);
        return await GenerateAuthResponse(user);
    }

    public async Task RequestPasswordResetAsync(RequestPasswordResetDto dto)
    {
        var email = dto.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new BadRequestException("Email is required");
        }

        var user = await FindUserByEmailAsync(email);
        if (user == null || !user.IsActive || string.IsNullOrWhiteSpace(user.Email))
        {
            await _auditLogService.WriteAsync(new AuditLogEntry
            {
                Action = "PasswordResetRequested",
                Succeeded = false,
                Details = $"Email={email}; Reason=NotFoundOrInactive"
            });

            _logger.LogInformation("Password reset requested for non-existing or inactive account: {Email}", email);
            return;
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetUrl = BuildPasswordResetUrl(user.Email, token);
        var body = BuildPasswordResetEmailBody(user.FullName, resetUrl);

        await _emailSender.SendAsync(user.Email, "Cap lai mat khau", body);
        await _auditLogService.WriteAsync(new AuditLogEntry
        {
            Action = "PasswordResetRequested",
            TargetUserId = user.Id,
            TargetUserName = user.UserName,
            Details = $"Email={user.Email}"
        });

        _logger.LogInformation("Password reset email sent to {Email}", user.Email);
    }

    public async Task ResetPasswordAsync(ResetPasswordDto dto)
    {
        var email = dto.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new BadRequestException("Email is required");
        }

        if (string.IsNullOrWhiteSpace(dto.Token))
        {
            throw new BadRequestException("Token is required");
        }

        if (string.IsNullOrWhiteSpace(dto.NewPassword))
        {
            throw new BadRequestException("NewPassword is required");
        }

        var user = await FindUserByEmailAsync(email);
        if (user == null)
        {
            await _auditLogService.WriteAsync(new AuditLogEntry
            {
                Action = "PasswordResetFailed",
                Succeeded = false,
                Details = $"Email={email}; Reason=UserNotFound"
            });

            throw new BadRequestException("Invalid password reset request");
        }

        var decodedToken = Uri.UnescapeDataString(dto.Token);
        var result = await _userManager.ResetPasswordAsync(user, decodedToken, dto.NewPassword);
        if (!result.Succeeded)
        {
            await _auditLogService.WriteAsync(new AuditLogEntry
            {
                Action = "PasswordResetFailed",
                Succeeded = false,
                TargetUserId = user.Id,
                TargetUserName = user.UserName,
                Details = "Invalid reset token or password policy failed"
            });

            throw new BadRequestException(string.Join(", ", result.Errors.Select(error => error.Description)));
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        await _userManager.SetLockoutEndDateAsync(user, null);

        await _auditLogService.WriteAsync(new AuditLogEntry
        {
            Action = "PasswordResetCompleted",
            TargetUserId = user.Id,
            TargetUserName = user.UserName
        });

        _logger.LogInformation("Password reset completed for {Email}", user.Email);
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        var tokenHash = TokenHelper.HashToken(token.Trim());
        var refreshToken = await _context.RefreshTokens
            .Include(item => item.User)
            .FirstOrDefaultAsync(item => item.TokenHash == tokenHash);

        if (refreshToken == null)
        {
            await _auditLogService.WriteAsync(new AuditLogEntry
            {
                Action = "RefreshTokenFailed",
                Succeeded = false,
                Details = "Token not found"
            });

            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        if (refreshToken.IsRevoked)
        {
            await RevokeActiveRefreshTokensAsync(refreshToken.UserId);
            await _auditLogService.WriteAsync(new AuditLogEntry
            {
                Action = "RefreshTokenReuseDetected",
                Succeeded = false,
                TargetUserId = refreshToken.UserId,
                TargetUserName = refreshToken.User?.UserName,
                Details = "Revoked refresh token was reused"
            });

            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        if (refreshToken.Expires < DateTime.UtcNow)
        {
            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _auditLogService.WriteAsync(new AuditLogEntry
            {
                Action = "RefreshTokenFailed",
                Succeeded = false,
                TargetUserId = refreshToken.UserId,
                TargetUserName = refreshToken.User?.UserName,
                Details = "Token expired"
            });

            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        refreshToken.IsRevoked = true;
        refreshToken.RevokedAt = DateTime.UtcNow;

        _logger.LogInformation("Refresh token used for user {UserId}", refreshToken.UserId);
        var response = await GenerateAuthResponse(refreshToken.User!, revokeExistingTokens: false, replacedToken: refreshToken);
        await _auditLogService.WriteAsync(new AuditLogEntry
        {
            Action = "RefreshTokenRotated",
            ActorUserId = refreshToken.UserId,
            ActorUserName = refreshToken.User?.UserName,
            TargetUserId = refreshToken.UserId,
            TargetUserName = refreshToken.User?.UserName
        });

        return response;
    }

    public async Task LogoutAsync(ClaimsPrincipal userPrincipal, string refreshToken)
    {
        var userId = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("Invalid token");
        }

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new UnauthorizedAccessException("Refresh token not found");
        }

        var tokenHash = TokenHelper.HashToken(refreshToken.Trim());
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(item => item.TokenHash == tokenHash);

        if (token == null)
        {
            throw new UnauthorizedAccessException("Refresh token not found");
        }

        if (token.UserId != userId)
        {
            throw new UnauthorizedAccessException("Forbidden");
        }

        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditLogService.WriteAsync(new AuditLogEntry
        {
            Action = "Logout",
            ActorUserId = userId,
            TargetUserId = userId,
            Details = "Refresh token revoked"
        });

        _logger.LogInformation("User {UserId} logged out", userId);
    }

    public async Task<(string UserId, string UserName, string Email, string FullName, List<string> Roles, List<string> Permissions, List<string> RolePermissions)> GetCurrentUserAsync(string userId)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == userId);

        if (user == null)
        {
            return (null!, null!, null!, null!, new List<string>(), new List<string>(), new List<string>());
        }

        var roles = (await _userManager.GetRolesAsync(user)).OrderBy(role => role).ToList();
        var rolePermissions = await _context.RolePermissions
            .Where(rolePermission => rolePermission.Role != null
                && rolePermission.Permission != null
                && roles.Contains(rolePermission.Role.Name!))
            .Select(rolePermission => rolePermission.Permission!.Name)
            .Distinct()
            .OrderBy(permission => permission)
            .ToListAsync();

        var permissions = await _permissionService.GetPermissionsAsync(user);

        return (
            user.Id,
            user.UserName ?? string.Empty,
            user.Email ?? string.Empty,
            user.FullName,
            roles,
            permissions.OrderBy(permission => permission).ToList(),
            rolePermissions);
    }

    private async Task<AuthResponseDto> GenerateAuthResponse(
        ApplicationUser user,
        bool revokeExistingTokens = true,
        RefreshToken? replacedToken = null)
    {
        var accessToken = await GenerateJwtAsync(user);

        if (revokeExistingTokens)
        {
            var oldTokens = await _context.RefreshTokens
                .Where(item => item.UserId == user.Id && !item.IsRevoked)
                .ToListAsync();

            foreach (var token in oldTokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
            }
        }

        var plainRefreshToken = TokenHelper.GenerateRefreshToken();
        var refreshTokenHash = TokenHelper.HashToken(plainRefreshToken);
        if (replacedToken != null)
        {
            replacedToken.ReplacedByTokenHash = refreshTokenHash;
        }

        var refreshToken = new RefreshToken
        {
            TokenHash = refreshTokenHash,
            Expires = DateTime.UtcNow.AddDays(RefreshTokenExpirationDays),
            UserId = user.Id
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = plainRefreshToken,
            Expiration = DateTime.UtcNow.AddHours(AccessTokenExpirationHours)
        };
    }

    private async Task RevokeActiveRefreshTokensAsync(string userId)
    {
        var activeTokens = await _context.RefreshTokens
            .Where(item => item.UserId == userId && !item.IsRevoked)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
        }

        if (activeTokens.Count > 0)
        {
            await _context.SaveChangesAsync();
        }
    }

    private async Task<string> GenerateJwtAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await _permissionService.GetPermissionsAsync(user);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(permissions.Select(permission => new Claim("permission", permission)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(AccessTokenExpirationHours),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string BuildPasswordResetUrl(string email, string token)
    {
        var clientUrl = (_config["App:ClientUrl"] ?? "http://localhost:5173").TrimEnd('/');
        var encodedEmail = Uri.EscapeDataString(email);
        var encodedToken = Uri.EscapeDataString(token);
        return $"{clientUrl}/reset-password?email={encodedEmail}&token={encodedToken}";
    }

    private async Task<ApplicationUser?> FindUserByEmailAsync(string email)
    {
        var normalizedEmail = _userManager.NormalizeEmail(email);
        var user = await _userManager.FindByEmailAsync(email)
            ?? await _userManager.Users.FirstOrDefaultAsync(item => item.Email == email);

        if (user != null && user.NormalizedEmail != normalizedEmail)
        {
            user.NormalizedEmail = normalizedEmail;
            await _userManager.UpdateAsync(user);
        }

        return user;
    }

    private static string BuildPasswordResetEmailBody(string fullName, string resetUrl)
    {
        var safeName = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(fullName) ? "ban" : fullName);
        var safeResetUrl = WebUtility.HtmlEncode(resetUrl);

        return $"""
            <div style="font-family:Arial,sans-serif;line-height:1.6;color:#111827">
              <h2>Cap lai mat khau</h2>
              <p>Xin chao {safeName},</p>
              <p>Ban vua gui yeu cau cap lai mat khau cho tai khoan QuanLyTaiKhoanAPI.</p>
              <p>
                <a href="{safeResetUrl}" style="display:inline-block;padding:10px 16px;background:#2563eb;color:#ffffff;text-decoration:none;border-radius:6px">
                  Dat lai mat khau
                </a>
              </p>
              <p>Neu nut tren khong hoat dong, hay sao chep lien ket sau vao trinh duyet:</p>
              <p><a href="{safeResetUrl}">{safeResetUrl}</a></p>
              <p>Neu ban khong yeu cau cap lai mat khau, vui long bo qua email nay.</p>
            </div>
            """;
    }
}
