using CinemaTicketBooking.Application;
using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;

namespace CinemaTicketBooking.Infrastructure.Persistence;

public class EFUnitOfWork(
    AppDbContext db,
    IMessageBus bus,
    IUserContext user,
    IServiceProvider serviceProvider) : IUnitOfWork
{
    private ICinemaRepository? _cinemas;
    private IMovieRepository? _movies;
    private IBookingRepository? _bookings;
    private ITicketRepository? _tickets;
    private IShowTimeRepository? _showTimes;
    private IScreenRepository? _screens;
    private IConcessionRepository? _concessions;
    private ICustomerRepository? _customers;
    private IPricingPolicyRepository? _pricingPolicies;
    private ISeatSelectionPolicyRepository? _seatSelectionPolicies;
    private IPaymentTransactionRepository? _paymentTransactions;
    private ISlideRepository? _slides;

    private ILoyaltyTierConfigurationRepository? _loyaltyTiers;

    public ICinemaRepository Cinemas => _cinemas ??= serviceProvider.GetRequiredService<ICinemaRepository>();
    public IMovieRepository Movies => _movies ??= serviceProvider.GetRequiredService<IMovieRepository>();
    public IBookingRepository Bookings => _bookings ??= serviceProvider.GetRequiredService<IBookingRepository>();
    public ITicketRepository Tickets => _tickets ??= serviceProvider.GetRequiredService<ITicketRepository>();
    public IShowTimeRepository ShowTimes => _showTimes ??= serviceProvider.GetRequiredService<IShowTimeRepository>();
    public IScreenRepository Screens => _screens ??= serviceProvider.GetRequiredService<IScreenRepository>();
    public IConcessionRepository Concessions => _concessions ??= serviceProvider.GetRequiredService<IConcessionRepository>();
    public ICustomerRepository Customers => _customers ??= serviceProvider.GetRequiredService<ICustomerRepository>();
    public IPricingPolicyRepository PricingPolicies => _pricingPolicies ??= serviceProvider.GetRequiredService<IPricingPolicyRepository>();
    public ISeatSelectionPolicyRepository SeatSelectionPolicies => _seatSelectionPolicies ??= serviceProvider.GetRequiredService<ISeatSelectionPolicyRepository>();
    public IPaymentTransactionRepository PaymentTransactions => _paymentTransactions ??= serviceProvider.GetRequiredService<IPaymentTransactionRepository>();
    public ISlideRepository Slides => _slides ??= serviceProvider.GetRequiredService<ISlideRepository>();
    public ILoyaltyTierConfigurationRepository LoyaltyTiers => _loyaltyTiers ??= serviceProvider.GetRequiredService<ILoyaltyTierConfigurationRepository>();

    public async Task CommitAsync(CancellationToken ct = default)
    {
        ApplyAuditingInformation();
        ApplySoftDelete();


        await DispatchEventsAsync(ct);
        await db.SaveChangesAsync(ct);
    }

    private async Task DispatchEventsAsync(CancellationToken ct)
    {
        var entitiesWithEvents = db.ChangeTracker
            .Entries<IDefaultEntity>()
            .Where(x => x.Entity.Events.Count > 0)
            .Select(x => x.Entity)
            .ToList();

        var events = entitiesWithEvents
            .SelectMany(x => x.Events)
            .ToList();

        foreach (var @event in events)
        {
            await bus.PublishAsync(@event);
        }

        foreach (var entity in entitiesWithEvents)
        {
            entity.ClearEvents();
        }
    }

    private void ApplyAuditingInformation()
    {
        var entries = db.ChangeTracker
            .Entries()
            .Where(entry => entry.Entity is IAuditable);

        foreach (var entry in entries)
        {
            var trackable = (IAuditable)entry.Entity;
            if (entry.State == EntityState.Added)
            {
                trackable.CreatedBy = user.UserName;
                if (entry.Entity is IDefaultEntity defaultEntity)
                {
                    defaultEntity.CreatedAt = DateTimeOffset.UtcNow;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                trackable.UpdatedBy = user.UserName;
                trackable.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }
    }

    private void ApplySoftDelete()
    {
        var entries = db.ChangeTracker.Entries<ISoftDeletable>();
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.Entity.DeletedAt = DateTimeOffset.UtcNow;
                entry.State = EntityState.Modified;
            }
        }
    }
}
