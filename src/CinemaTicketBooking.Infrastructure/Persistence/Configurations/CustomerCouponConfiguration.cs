using CinemaTicketBooking.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaTicketBooking.Infrastructure.Persistence.Configurations;

public class CustomerCouponConfiguration : IEntityTypeConfiguration<CustomerCoupon>
{
    public void Configure(EntityTypeBuilder<CustomerCoupon> builder)
    {
        builder.ToTable("customer_coupons");
        builder.ConfigureAggregateRoot();

        builder.Property(x => x.CustomerId).IsRequired();
        builder.HasIndex(x => x.CustomerId);

        builder.Property(x => x.CouponTemplateId).IsRequired(false);

        builder.Property(x => x.CouponCode)
            .HasMaxLength(MaxLengthConsts.CouponCode)
            .IsRequired();
        builder.HasIndex(x => new { x.CustomerId, x.CouponCode }).IsUnique();

        builder.Property(x => x.DiscountType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.DiscountValue)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.MaxDiscountAmount)
            .HasColumnType("decimal(18,2)")
            .IsRequired(false);

        builder.Property(x => x.Scope)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.UsageCount).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.MaxUsage).IsRequired().HasDefaultValue(1);
        builder.Property(x => x.IssuedAt).IsRequired();
        builder.Property(x => x.UsedAt).IsRequired(false);
        builder.Property(x => x.ExpiresAt).IsRequired();
    }
}