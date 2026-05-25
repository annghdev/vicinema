using Microsoft.AspNetCore.Http;

namespace CinemaTicketBooking.Infrastructure.Auth;

/// <summary>
/// Refresh token lifetime and cookie options.
/// </summary>
public sealed class RefreshTokenOptions
{
    public const string SectionName = "RefreshToken";

    public int DaysValid { get; set; } = 14;
    public string CookieName { get; set; } = "rt";

    /// <summary>
    /// Cookie path (use <c>/</c> if the API and refresh endpoint are not under the same prefix in all environments).
    /// </summary>
    public string CookiePath { get; set; } = "/";

    /// <summary>
    /// When true, sets Secure flag on the refresh cookie.
    /// </summary>
    public bool CookieSecure { get; set; } = true;

    public SameSiteMode CookieSameSite { get; set; } = SameSiteMode.Lax;
}
