namespace CinemaTicketBooking.Application.Common.Auth;

/// <summary>
/// Static permission codes stored in <c>AspNetRoleClaims</c> (claim type <see cref="AuthClaimTypes.Permission"/>).
/// </summary>
public static class Permissions
{
    // === Bookings ===
    public const string BookingsViewAll = "bookings.view_all";
    public const string BookingsManage = "bookings.manage";

    // === Accounts / Identity ===
    public const string AccountsView = "accounts.view";
    public const string AccountsLock = "accounts.lock";
    public const string AccountsUnlock = "accounts.unlock";
    public const string AccountsManage = "accounts.manage";

    // === Cinemas & Screens ===
    public const string CinemasView = "cinemas.view";
    public const string CinemasManage = "cinemas.manage";
    public const string ScreensView = "screens.view";
    public const string ScreensManage = "screens.manage";

    // === Movies & ShowTimes ===
    public const string MoviesView = "movies.view";
    public const string MoviesManage = "movies.manage";
    public const string ShowTimesView = "showtimes.view";
    public const string ShowTimesManage = "showtimes.manage";

    // === Policies & Concessions ===
    public const string PricingPoliciesView = "pricing_policies.view";
    public const string PricingPoliciesManage = "pricing_policies.manage";
    public const string SeatSelectionPoliciesView = "seat_selection_policies.view";
    public const string SeatSelectionPoliciesManage = "seat_selection_policies.manage";
    public const string ConcessionsView = "concessions.view";
    public const string ConcessionsManage = "concessions.manage";

    // === Customers ===
    public const string CustomersView = "customers.view";
    public const string CustomersManage = "customers.manage";

    // === Loyalty ===
    public const string LoyaltyView = "loyalty.view";
    public const string LoyaltyManage = "loyalty.manage";

    // === Reports & Analysis ===
    public const string ReportsView = "reports.view";
    public const string AnalysisView = "analysis.view";
}
