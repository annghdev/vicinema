using CinemaTicketBooking.Domain;
using CinemaTicketBooking.Infrastructure.Auth;
using CinemaTicketBooking.Infrastructure.Persistence.Configurations;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<Account, Role, Guid>(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.AddInterceptors(new UtcSaveChangesInterceptor());
    }
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Cinema> Cinemas => Set<Cinema>();
    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<Concession> Concessions => Set<Concession>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<PricingPolicy> PricingPolicies => Set<PricingPolicy>();
    public DbSet<Screen> Screens => Set<Screen>();
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<ShowTime> ShowTimes => Set<ShowTime>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<SeatSelectionPolicy> SeatSelectionPolicies => Set<SeatSelectionPolicy>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingTicket> BookingTickets => Set<BookingTicket>();
    public DbSet<BookingConcession> BookingConcessions => Set<BookingConcession>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Slide> Slides => Set<Slide>();
    public DbSet<LoyaltyTierConfiguration> LoyaltyTierConfigurations => Set<LoyaltyTierConfiguration>();
    public DbSet<CouponTemplate> CouponTemplates => Set<CouponTemplate>();
    public DbSet<CustomerCoupon> CustomerCoupons => Set<CustomerCoupon>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ConfigureIdentityTextColumns();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        modelBuilder.ApplySoftDeleteQueryFilters();
    }
}
