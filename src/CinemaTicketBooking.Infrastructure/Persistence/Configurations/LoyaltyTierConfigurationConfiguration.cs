using CinemaTicketBooking.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaTicketBooking.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF configuration for <see cref="LoyaltyTierConfiguration"/>.
/// </summary>
public class LoyaltyTierConfigurationConfiguration : IEntityTypeConfiguration<LoyaltyTierConfiguration>
{
    public void Configure(EntityTypeBuilder<LoyaltyTierConfiguration> builder)
    {
        builder.ToTable("loyalty_tier_configurations");
        builder.ConfigureAggregateRoot();

        builder.Property(x => x.Tier)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.HasIndex(x => x.Tier).IsUnique();

        builder.Property(x => x.Name).HasMaxLength(MaxLengthConsts.Name).IsRequired();
        builder.Property(x => x.MinPoints).IsRequired();
        builder.Property(x => x.MaxPoints).IsRequired(false);
        builder.Property(x => x.TicketDiscountPercent)
            .HasColumnType("decimal(5,2)")
            .IsRequired();
        builder.Property(x => x.ConcessionDiscountPercent)
            .HasColumnType("decimal(5,2)")
            .IsRequired();
        builder.Property(x => x.Description).HasMaxLength(MaxLengthConsts.Description);
        builder.Property(x => x.IsActive).IsRequired();

        builder.Property(x => x.CouponTemplateId).IsRequired(false);
        builder.HasOne(x => x.CouponTemplate)
            .WithMany()
            .HasForeignKey(x => x.CouponTemplateId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
