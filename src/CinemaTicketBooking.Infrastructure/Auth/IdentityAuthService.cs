using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Common.Auth;
using CinemaTicketBooking.Application.Features;
using CinemaTicketBooking.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CinemaTicketBooking.Infrastructure.Auth;

/// <summary>
/// Coordinates JWT access tokens, rotating refresh tokens (HttpOnly cookie), and Identity user operations.
/// </summary>
public sealed class IdentityAuthService(
    UserManager<Account> userManager,
    RoleManager<Role> roleManager,
    AppDbContext db,
    IOptions<JwtOptions> jwtOptions,
    IOptions<RefreshTokenOptions> refreshOptions,
    IOptions<TestAuthOptions> testAuthOptions,
    IEmailSender emailSender,
    IHttpContextAccessor httpContextAccessor) : IAuthService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;
    private readonly RefreshTokenOptions _refresh = refreshOptions.Value;
    private readonly TestAuthOptions _testAuth = testAuthOptions.Value;

    /// <inheritdoc />
    public async Task<AuthTokenResponse?> IssueTokensAsync(Guid accountId, string? remoteIp, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(accountId.ToString());
        if (user is null || await userManager.IsLockedOutAsync(user))
            return null;

        var accessToken = await CreateAccessTokenAsync(user, cancellationToken);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_jwt.AccessTokenMinutes);

        // Register/login flows may leave unrelated tracked entities in this scoped DbContext.
        // Persisting refresh token should not flush those entities, which can trigger false
        // optimistic concurrency failures on unrelated rows.
        db.ChangeTracker.Clear();

        var rawRefresh = GenerateRefreshTokenRaw();
        var hash = HashRefreshToken(rawRefresh);
        var refreshEntity = new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            AccountId = user.Id,
            TokenHash = hash,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_refresh.DaysValid),
            CreatedFromIp = remoteIp,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.RefreshTokens.Add(refreshEntity);
        await db.SaveChangesAsync(cancellationToken);

        AppendRefreshCookie(rawRefresh);

        return new AuthTokenResponse(
            accessToken,
            expiresAt,
            user.Id,
            _testAuth.ExposeRefreshTokenInJson ? rawRefresh : null);
    }

    /// <inheritdoc />
    public async Task<AuthTokenResponse?> RefreshAsync(string? refreshTokenRaw, string? remoteIp, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenRaw))
            return null;

        var hash = HashRefreshToken(refreshTokenRaw);
        var existing = await db.RefreshTokens
            .Include(x => x.Account)
            .FirstOrDefaultAsync(
                x => x.TokenHash == hash
                    && x.RevokedAt == null
                    && x.DeletedAt == null,
                cancellationToken);

        if (existing is null || existing.ExpiresAt < DateTimeOffset.UtcNow)
            return null;

        var user = existing.Account ?? await userManager.FindByIdAsync(existing.AccountId.ToString());
        if (user is null || await userManager.IsLockedOutAsync(user))
            return null;

        // 1. Revoke old token (rotation).
        existing.RevokedAt = DateTimeOffset.UtcNow;

        var rawRefresh = GenerateRefreshTokenRaw();
        var newHash = HashRefreshToken(rawRefresh);
        var newEntity = new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            AccountId = existing.AccountId,
            TokenHash = newHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_refresh.DaysValid),
            CreatedFromIp = remoteIp,
            CreatedAt = DateTimeOffset.UtcNow
        };
        existing.ReplacedByTokenId = newEntity.Id;
        db.RefreshTokens.Add(newEntity);

        // 2. Persist rotation — handle concurrent refresh race.
        // When multiple requests refresh the same token simultaneously, the loser
        // hits a DbUpdateConcurrencyException because PostgreSQL xmin changed.
        // Return null (→ 401) instead of letting it bubble up as 409 Conflict.
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return null;
        }

        var accessToken = await CreateAccessTokenAsync(user, cancellationToken);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_jwt.AccessTokenMinutes);

        AppendRefreshCookie(rawRefresh);

        return new AuthTokenResponse(
            accessToken,
            expiresAt,
            user.Id,
            _testAuth.ExposeRefreshTokenInJson ? rawRefresh : null);
    }

    /// <inheritdoc />
    public async Task LogoutAsync(string? refreshTokenRaw, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(refreshTokenRaw))
        {
            var hash = HashRefreshToken(refreshTokenRaw);
            var existing = await db.RefreshTokens.FirstOrDefaultAsync(
                x => x.TokenHash == hash && x.RevokedAt == null && x.DeletedAt == null,
                cancellationToken);
            if (existing is not null)
                existing.RevokedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        ClearRefreshCookie();
    }

    /// <inheritdoc />
    public async Task RevokeAllRefreshTokensAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        await db.RefreshTokens
            .Where(x => x.AccountId == accountId && x.RevokedAt == null && x.DeletedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAt, now), cancellationToken);
    }

    /// <inheritdoc />
    public async Task RequestPasswordResetAsync(string email, string resetPageBaseUrl, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            // Do not reveal whether the email exists.
            return;
        }

        var code = await userManager.GeneratePasswordResetTokenAsync(user);
        var safeCode = Uri.EscapeDataString(code);
        var link = $"{resetPageBaseUrl.TrimEnd('/')}?userId={user.Id}&code={safeCode}";

        await emailSender.SendEmailAsync(
            user.Email ?? email,
            "Password reset",
            $"<p>Reset your password using this link (valid for a limited time):</p><p><a href=\"{link}\">Reset password</a></p>",
            user.UserName,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<IdentityResult> ResetPasswordAsync(Guid userId, string resetCode, string newPassword, CancellationToken cancellationToken = default)
    {
        return ResetPasswordCoreAsync(userId, resetCode, newPassword, cancellationToken);
    }

    private async Task<IdentityResult> ResetPasswordCoreAsync(Guid userId, string resetCode, string newPassword, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return IdentityResult.Failed(new IdentityError { Description = "User not found." });

        var result = await userManager.ResetPasswordAsync(user, resetCode, newPassword);
        if (result.Succeeded)
            await RevokeAllRefreshTokensAsync(user.Id, cancellationToken);
        return result;
    }

    /// <inheritdoc />
    public async Task<IdentityResult> DeleteAccountAsync(Guid accountId, string password, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(accountId.ToString());
        if (user is null)
            return IdentityResult.Failed(new IdentityError { Description = "User not found." });

        if (!await userManager.CheckPasswordAsync(user, password))
            return IdentityResult.Failed(new IdentityError { Description = "Invalid password." });

        await RevokeAllRefreshTokensAsync(user.Id, cancellationToken);
        user.DeletedAt = DateTimeOffset.UtcNow;
        user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
        await db.SaveChangesAsync(cancellationToken);
        return await userManager.UpdateAsync(user);
    }

    /// <inheritdoc />
    public async Task LockAccountAsync(Guid accountId, DateTimeOffset lockoutEnd, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(accountId.ToString());
        if (user is null)
            return;

        await userManager.SetLockoutEnabledAsync(user, true);
        await userManager.SetLockoutEndDateAsync(user, lockoutEnd);
        await RevokeAllRefreshTokensAsync(accountId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UnlockAccountAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(accountId.ToString());
        if (user is null)
            return;

        await userManager.SetLockoutEndDateAsync(user, null);
        await userManager.ResetAccessFailedCountAsync(user);
    }

    /// <inheritdoc />
    public async Task<IdentityResult> CreateAccountAsync(string email, string userName, string password, List<string> roles, CancellationToken cancellationToken = default)
    {
        var account = new Account
        {
            Id = Guid.CreateVersion7(),
            UserName = userName,
            Email = email,
            EmailConfirmed = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var result = await userManager.CreateAsync(account, password);
        if (!result.Succeeded) return result;

        foreach (var roleName in roles)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                await userManager.AddToRoleAsync(account, roleName);
            }
        }

        return IdentityResult.Success;
    }

    /// <inheritdoc />
    public async Task<SystemAccountDetailDto?> GetAccountDetailsAsync(Guid accountId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(accountId.ToString());
        if (user is null) return null;

        var roles = await userManager.GetRolesAsync(user);
        var claims = await userManager.GetClaimsAsync(user);
        var permissions = claims
            .Where(c => c.Type == AuthClaimTypes.Permission)
            .Select(c => c.Value)
            .ToList();

        return new SystemAccountDetailDto(
            Id: user.Id,
            UserName: user.UserName ?? string.Empty,
            Email: user.Email ?? string.Empty,
            Roles: roles.ToList(),
            Permissions: permissions);
    }

    /// <inheritdoc />
    public async Task<IdentityResult> UpdateAccountRolesAndClaimsAsync(Guid accountId, List<string> roles, List<string> permissions, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(accountId.ToString());
        if (user is null) return IdentityResult.Failed(new IdentityError { Description = "User not found" });

        // 1. Sync roles
        var currentRoles = await userManager.GetRolesAsync(user);
        var rolesToRemove = currentRoles.Except(roles).ToList();
        var rolesToAdd = roles.Except(currentRoles).ToList();

        if (rolesToRemove.Any()) await userManager.RemoveFromRolesAsync(user, rolesToRemove);
        if (rolesToAdd.Any()) await userManager.AddToRolesAsync(user, rolesToAdd);

        // 2. Sync permission claims
        var currentClaims = await userManager.GetClaimsAsync(user);
        var currentPermissions = currentClaims
            .Where(c => c.Type == AuthClaimTypes.Permission)
            .ToList();

        // Remove old permissions
        foreach (var claim in currentPermissions)
        {
            if (!permissions.Contains(claim.Value))
            {
                await userManager.RemoveClaimAsync(user, claim);
            }
        }

        // Add new permissions
        foreach (var perm in permissions)
        {
            if (!currentPermissions.Any(c => c.Value == perm))
            {
                await userManager.AddClaimAsync(user, new Claim(AuthClaimTypes.Permission, perm));
            }
        }

        await RevokeAllRefreshTokensAsync(accountId, ct);
        return IdentityResult.Success;
    }

    /// <inheritdoc />
    public async Task<string> AdminResetPasswordAsync(Guid accountId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(accountId.ToString());
        if (user is null) throw new Exception("Tài khoản không tồn tại.");

        // Generate a random password: 8 chars base64 + "aA1!" to satisfy most policies
        var bytes = RandomNumberGenerator.GetBytes(9);
        var base64 = Convert.ToBase64String(bytes).Substring(0, 8);
        var newPassword = base64 + "aA1!"; 

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, newPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new Exception($"Không thể đặt lại mật khẩu: {errors}");
        }

        await RevokeAllRefreshTokensAsync(accountId, ct);
        return newPassword;
    }

    /// <inheritdoc />
    public void ClearRefreshCookie()
    {
        var ctx = httpContextAccessor.HttpContext;
        if (ctx is null)
            return;

        ctx.Response.Cookies.Delete(_refresh.CookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = _refresh.CookieSecure,
            SameSite = _refresh.CookieSameSite,
            Path = _refresh.CookiePath
        });
    }

    private void AppendRefreshCookie(string rawRefresh)
    {
        var ctx = httpContextAccessor.HttpContext;
        if (ctx is null)
            return;

        ctx.Response.Cookies.Append(_refresh.CookieName, rawRefresh, new CookieOptions
        {
            HttpOnly = true,
            Secure = _refresh.CookieSecure,
            SameSite = _refresh.CookieSameSite,
            Path = _refresh.CookiePath,
            MaxAge = TimeSpan.FromDays(_refresh.DaysValid),
            IsEssential = true
        });
    }

    private async Task<string> CreateAccessTokenAsync(Account user, CancellationToken cancellationToken)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty)
        };

        if (user.CustomerId is { } cid)
            claims.Add(new Claim(AuthClaimTypes.CustomerId, cid.ToString()));

        foreach (var roleName in await userManager.GetRolesAsync(user))
        {
            claims.Add(new Claim(ClaimTypes.Role, roleName));
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is not null)
            {
                var roleClaims = await roleManager.GetClaimsAsync(role);
                claims.AddRange(roleClaims);
            }
        }

        foreach (var c in await userManager.GetClaimsAsync(user))
            claims.Add(c);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes);
        var token = new JwtSecurityToken(
            _jwt.Issuer,
            _jwt.Audience,
            claims,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshTokenRaw()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }

    private static string HashRefreshToken(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
