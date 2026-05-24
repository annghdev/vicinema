using CinemaTicketBooking.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaTicketBooking.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");
        builder.ConfigureAggregateRoot();

        builder.Property(x => x.Name).HasMaxLength(MaxLengthConsts.Name).IsRequired();
        builder.Property(x => x.SessionId).HasMaxLength(MaxLengthConsts.SessionId).IsRequired();
        builder.Property(x => x.PhoneNumber).HasMaxLength(MaxLengthConsts.PhoneNumber);
        builder.Property(x => x.Email).HasMaxLength(MaxLengthConsts.Email);
        builder.Property(x => x.IsRegistered).IsRequired();

        builder.Property(x => x.LoyaltyTier)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired()
            .HasDefaultValue(LoyaltyTier.Bronze);

        builder.Property(x => x.AccumulatedPoints)
            .IsRequired()
            .HasDefaultValue(0);
    }
}
