using System.Security.Claims;
using System.Security.Cryptography;
using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Common.Auth;
using CinemaTicketBooking.Application.Features.Auth.Commands;
using CinemaTicketBooking.Domain;
using CinemaTicketBooking.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Wolverine;

namespace CinemaTicketBooking.WebServer.ApiEndpoints;

/// <summary>
/// Maps authentication and account management minimal APIs.
/// </summary>
public static class AuthEndpoints
{
    /// <summary>
    /// Registers <c>/api/auth</c> routes.
    /// </summary>
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth")
            .RequireRateLimiting("sliding").WithTags("Auth");

        group.MapPost("/register", RegisterAsync)
            .AllowAnonymous();

        group.MapPost("/login", LoginAsync)
            .AllowAnonymous();

        group.MapPost("/refresh", RefreshAsync)
            .AllowAnonymous();

        group.MapPost("/logout", LogoutAsync)
            .AllowAnonymous();

        group.MapGet("/me", MeAsync)
            .RequireAuthorization();

        group.MapPost("/forgot-password", ForgotPasswordAsync)
            .AllowAnonymous();

        group.MapPost("/reset-password", ResetPasswordAsync)
            .AllowAnonymous();

        group.MapDelete("/account", DeleteAccountAsync)
            .RequireAuthorization();

        group.MapPost("/change-password", ChangePasswordAsync)
            .RequireAuthorization();

        group.MapGet("/external/google", GoogleChallenge)
            .AllowAnonymous();

        group.MapGet("/external/google/complete", GoogleCompleteAsync)
            .AllowAnonymous();

        group.MapGet("/external/facebook", FacebookChallenge)
            .AllowAnonymous();

        group.MapGet("/external/facebook/complete", FacebookCompleteAsync)
            .AllowAnonymous();

        var admin = group.MapGroup("/admin")
            .RequireAuthorization();

        admin.MapPost("/accounts/{accountId:guid}/lock", LockAccountAsync)
            .RequireAuthorization(Permissions.AccountsLock);

        admin.MapPost("/accounts/{accountId:guid}/unlock", UnlockAccountAsync)
            .RequireAuthorization(Permissions.AccountsUnlock);
    }

    private static async Task<IResult> RegisterAsync(
        [FromBody] RegisterRequest dto,
        UserManager<Account> userManager,
        IMessageBus bus,
        IAuthService auth,
        HttpContext http,
        CancellationToken ct)
    {
        var user = new Account
        {
            Id = Guid.CreateVersion7(),
            UserName = dto.Email,
            Email = dto.Email,
            EmailConfirmed = true
        };

        var create = await userManager.CreateAsync(user, dto.Password);
        if (!create.Succeeded)
            return Results.ValidationProblem(create.ErrorDictionary());

        var role = await userManager.AddToRoleAsync(user, RoleNames.Customer);
        if (!role.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return Results.ValidationProblem(role.ErrorDictionary());
        }

        try
        {
            var customerId = await bus.InvokeAsync<Guid>(
                new ProvisionCustomerForAccountCommand
                {
                    AccountId = user.Id,
                    Email = dto.Email,
                    Name = dto.Name,
                    PhoneNumber = dto.PhoneNumber,
                    SessionId = dto.SessionId,
                    CorrelationId = http.TraceIdentifier
                },
                ct);
            user.CustomerId = customerId;
        }
        catch
        {
            await userManager.DeleteAsync(user);
            throw;
        }

        user = await userManager.FindByIdAsync(user.Id.ToString()) ?? user;

        var tokens = await auth.IssueTokensAsync(user.Id, http.Connection.RemoteIpAddress?.ToString(), ct);
        if (tokens is null)
            return Results.Unauthorized();

        return Results.Ok(new AuthTokenApiResponse(
            tokens.AccessToken,
            tokens.AccessTokenExpiresAtUtc,
            tokens.AccountId,
            tokens.RefreshTokenPlaintext));
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginRequest dto,
        SignInManager<Account> signInManager,
        UserManager<Account> userManager,
        IAuthService auth,
        HttpContext http,
        CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(dto.Email);
        if (user is null)
            return Results.Unauthorized();

        var check = await signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);
        if (!check.Succeeded)
            return Results.Unauthorized();

        var tokens = await auth.IssueTokensAsync(user.Id, http.Connection.RemoteIpAddress?.ToString(), ct);
        if (tokens is null)
            return Results.Unauthorized();

        return Results.Ok(new AuthTokenApiResponse(
            tokens.AccessToken,
            tokens.AccessTokenExpiresAtUtc,
            tokens.AccountId,
            tokens.RefreshTokenPlaintext));
    }

    private static async Task<IResult> RefreshAsync(
        IOptions<RefreshTokenOptions> refreshOptions,
        IAuthService auth,
        HttpContext http,
        CancellationToken ct)
    {
        var cookieName = refreshOptions.Value.CookieName;
        http.Request.Cookies.TryGetValue(cookieName, out var raw);

        var tokens = await auth.RefreshAsync(raw, http.Connection.RemoteIpAddress?.ToString(), ct);
        if (tokens is null)
            return Results.Unauthorized();

        return Results.Ok(new AuthTokenApiResponse(
            tokens.AccessToken,
            tokens.AccessTokenExpiresAtUtc,
            tokens.AccountId,
            tokens.RefreshTokenPlaintext));
    }

    private static async Task<IResult> LogoutAsync(
        IOptions<RefreshTokenOptions> refreshOptions,
        IAuthService auth,
        HttpContext http,
        CancellationToken ct)
    {
        http.Request.Cookies.TryGetValue(refreshOptions.Value.CookieName, out var raw);
        await auth.LogoutAsync(raw, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> MeAsync(
        ClaimsPrincipal principal,
        UserManager<Account> userManager,
        IUnitOfWork uow,
        CancellationToken ct)
    {
        var accountIdRaw = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(accountIdRaw, out var accountId))
            return Results.Unauthorized();

        var account = await userManager.FindByIdAsync(accountId.ToString());
        if (account is null)
            return Results.NotFound();

        Customer? customer = null;
        if (account.CustomerId is { } customerId)
        {
            customer = await uow.Customers.GetByIdAsync(customerId, ct);
        }

        var displayName = customer?.Name;
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = account.UserName ?? account.Email ?? "Khách hàng";
        }

        return Results.Ok(new AuthProfileApiResponse(
            account.Id,
            account.CustomerId,
            displayName,
            account.Email ?? customer?.Email,
            account.AvatarUrl,
            customer?.PhoneNumber));
    }

    private static async Task<IResult> ForgotPasswordAsync(
        [FromBody] ForgotPasswordRequest dto,
        IAuthService auth,
        IConfiguration config,
        CancellationToken ct)
    {
        var baseUrl = config["App:PublicBaseUrl"] ?? "https://localhost";
        await auth.RequestPasswordResetAsync(dto.Email, $"{baseUrl.TrimEnd('/')}/reset-password", ct);
        return Results.Accepted();
    }

    private static async Task<IResult> ResetPasswordAsync(
        [FromBody] ResetPasswordRequest dto,
        IAuthService auth,
        CancellationToken ct)
    {
        var result = await auth.ResetPasswordAsync(dto.UserId, dto.Code, dto.NewPassword, ct);
        if (!result.Succeeded)
            return Results.ValidationProblem(result.ErrorDictionary());

        return Results.NoContent();
    }

    private static async Task<IResult> DeleteAccountAsync(
        [FromBody] DeleteAccountRequest dto,
        UserManager<Account> userManager,
        ClaimsPrincipal principal,
        IAuthService auth,
        CancellationToken ct)
    {
        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(id, out var userId))
            return Results.Unauthorized();

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Results.NotFound();

        var result = await auth.DeleteAccountAsync(user.Id, dto.Password, ct);
        if (!result.Succeeded)
            return Results.ValidationProblem(result.ErrorDictionary());

        auth.ClearRefreshCookie();
        return Results.NoContent();
    }

    /// <summary>
    /// Changes the password of the authenticated user.
    /// </summary>
    private static async Task<IResult> ChangePasswordAsync(
        [FromBody] ChangePasswordRequest dto,
        UserManager<Account> userManager,
        ClaimsPrincipal principal,
        CancellationToken ct)
    {
        // 1. Resolve user ID from claims principal
        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(id, out var userId))
            return Results.Unauthorized();

        // 2. Fetch the corresponding user account
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Results.NotFound();

        // 3. Update the password using ASP.NET Core Identity
        var result = await userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
            return Results.ValidationProblem(result.ErrorDictionary());

        return Results.NoContent();
    }

    private static IResult GoogleChallenge(string? returnUrl, string? sessionId, IConfiguration config)
    {
        var redirect = "/api/auth/external/google/complete";
        var props = new AuthenticationProperties { RedirectUri = redirect };
        if (!string.IsNullOrEmpty(returnUrl))
            props.Items["returnUrl"] = returnUrl;
        if (!string.IsNullOrEmpty(sessionId))
            props.Items["sessionId"] = sessionId;

        return Results.Challenge(properties: props, authenticationSchemes: [GoogleDefaults.AuthenticationScheme]);
    }

    private static IResult FacebookChallenge(string? returnUrl, string? sessionId, IConfiguration config)
    {
        var redirect = "/api/auth/external/facebook/complete";
        var props = new AuthenticationProperties { RedirectUri = redirect };
        if (!string.IsNullOrEmpty(returnUrl))
            props.Items["returnUrl"] = returnUrl;
        if (!string.IsNullOrEmpty(sessionId))
            props.Items["sessionId"] = sessionId;

        return Results.Challenge(properties: props, authenticationSchemes: [FacebookDefaults.AuthenticationScheme]);
    }

    private static Task<IResult> GoogleCompleteAsync(
        SignInManager<Account> signInManager,
        UserManager<Account> userManager,
        IMessageBus bus,
        IAuthService auth,
        IConfiguration config,
        HttpContext http,
        CancellationToken ct)
        => ExternalCompleteAsync(signInManager, userManager, bus, auth, config, http, ct);

    private static Task<IResult> FacebookCompleteAsync(
        SignInManager<Account> signInManager,
        UserManager<Account> userManager,
        IMessageBus bus,
        IAuthService auth,
        IConfiguration config,
        HttpContext http,
        CancellationToken ct)
        => ExternalCompleteAsync(signInManager, userManager, bus, auth, config, http, ct);

    private static async Task<IResult> ExternalCompleteAsync(
        SignInManager<Account> signInManager,
        UserManager<Account> userManager,
        IMessageBus bus,
        IAuthService auth,
        IConfiguration config,
        HttpContext http,
        CancellationToken ct)
    {
        var frontendBaseUrl = config["FrontendOrigin"] ?? "http://localhost:5173";
        
        // 1. Manually authenticate the external scheme
        var authResult = await http.AuthenticateAsync(IdentityConstants.ExternalScheme);
        if (!authResult.Succeeded)
        {
            return Results.Redirect($"{frontendBaseUrl.TrimEnd('/')}/auth-callback?error=external_auth_failed");
        }

        // 2. Extract info from the authentication result
        authResult.Properties.Items.TryGetValue("LoginProvider", out var loginProvider);
        loginProvider ??= "Google";
        var providerKey = authResult.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (string.IsNullOrEmpty(providerKey))
        {
            return Results.Redirect($"{frontendBaseUrl.TrimEnd('/')}/auth-callback?error=provider_key_missing");
        }

        authResult.Properties.Items.TryGetValue("returnUrl", out var returnUrl);
        authResult.Properties.Items.TryGetValue("sessionId", out var sessionId);

        var signIn = await signInManager.ExternalLoginSignInAsync(
            loginProvider,
            providerKey,
            isPersistent: false,
            bypassTwoFactor: true);

        Account? user;
        if (signIn.Succeeded)
        {
            user = await userManager.FindByLoginAsync(loginProvider, providerKey);
            if (user is null)
                return Results.Redirect($"{frontendBaseUrl.TrimEnd('/')}/auth-callback?error=account_not_found");
        }
        else
        {
            var info = new ExternalLoginInfo(authResult.Principal, loginProvider, providerKey, loginProvider);
            var email = authResult.Principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email))
                return Results.Redirect($"{frontendBaseUrl.TrimEnd('/')}/auth-callback?error=email_not_provided");

            user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                var name = info.Principal.FindFirstValue(ClaimTypes.Name) ?? email.Split('@')[0];
                user = new Account
                {
                    Id = Guid.CreateVersion7(),
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    AvatarUrl = authResult.Principal.FindFirstValue("picture") 
                                ?? authResult.Principal.FindFirstValue("image")
                                ?? authResult.Principal.FindFirstValue("avatar_url")
                                ?? authResult.Principal.FindFirstValue(ClaimTypes.Uri) // Some providers use this for picture
                };
                var randomPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) + "aA1!";
                var created = await userManager.CreateAsync(user, randomPassword);
                if (!created.Succeeded)
                    return Results.Redirect($"{frontendBaseUrl.TrimEnd('/')}/auth-callback?error=account_creation_failed");

                var addLogin = await userManager.AddLoginAsync(user, info);
                if (!addLogin.Succeeded)
                {
                    await userManager.DeleteAsync(user);
                    return Results.Redirect($"{frontendBaseUrl.TrimEnd('/')}/auth-callback?error=external_link_failed");
                }

                var role = await userManager.AddToRoleAsync(user, RoleNames.Customer);
                if (!role.Succeeded)
                {
                    await userManager.DeleteAsync(user);
                    return Results.Redirect($"{frontendBaseUrl.TrimEnd('/')}/auth-callback?error=role_assignment_failed");
                }

                try
                {
                    var customerId = await bus.InvokeAsync<Guid>(
                        new ProvisionCustomerForAccountCommand
                        {
                            AccountId = user.Id,
                            Email = email,
                            Name = name,
                            PhoneNumber = authResult.Principal.FindFirstValue(ClaimTypes.MobilePhone) ?? "—",
                            SessionId = sessionId,
                            CorrelationId = http.TraceIdentifier
                        },
                        ct);
                    user.CustomerId = customerId;
                }
                catch
                {
                    await userManager.DeleteAsync(user);
                    throw;
                }
            }
            else
            {
                var addLogin = await userManager.AddLoginAsync(user, info);
                if (!addLogin.Succeeded)
                    return Results.Redirect($"{frontendBaseUrl.TrimEnd('/')}/login?error=external_link_failed");
            }
        }

        user = await userManager.FindByIdAsync(user.Id.ToString()) ?? user;

        var tokens = await auth.IssueTokensAsync(user.Id, http.Connection.RemoteIpAddress?.ToString(), ct);
        if (tokens is null)
            return Results.Redirect($"{frontendBaseUrl.TrimEnd('/')}/login?error=token_issuance_failed");

        var callbackUrl = $"{frontendBaseUrl.TrimEnd('/')}/auth-callback" +
                         $"?accessToken={tokens.AccessToken}" +
                         $"&expiresAt={tokens.AccessTokenExpiresAtUtc.ToUnixTimeSeconds()}" +
                         $"&accountId={tokens.AccountId}" +
                         (string.IsNullOrEmpty(returnUrl) ? "" : $"&returnUrl={Uri.EscapeDataString(returnUrl)}");

        return Results.Redirect(callbackUrl);
    }

    private static async Task<IResult> LockAccountAsync(
        Guid accountId,
        [FromBody] LockAccountRequest? body,
        IAuthService auth,
        CancellationToken ct)
    {
        var end = body?.LockoutEndUtc ?? DateTimeOffset.UtcNow.AddYears(100);
        await auth.LockAccountAsync(accountId, end, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> UnlockAccountAsync(
        Guid accountId,
        IAuthService auth,
        CancellationToken ct)
    {
        await auth.UnlockAccountAsync(accountId, ct);
        return Results.NoContent();
    }

    private static IDictionary<string, string[]> ErrorDictionary(this IdentityResult result)
    {
        return new Dictionary<string, string[]>
        {
            ["errors"] = result.Errors.Select(e => $"{e.Code}: {e.Description}").ToArray()
        };
    }
}

