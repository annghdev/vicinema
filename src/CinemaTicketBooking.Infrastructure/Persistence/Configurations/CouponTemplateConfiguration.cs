using CinemaTicketBooking.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaTicketBooking.Infrastructure.Persistence.Configurations;

public class CouponTemplateConfiguration : IEntityTypeConfiguration<CouponTemplate>
{
    public void Configure(EntityTypeBuilder<CouponTemplate> builder)
    {
        builder.ToTable("coupon_templates");
        builder.ConfigureAggregateRoot();

        builder.Property(x => x.Code)
            .HasMaxLength(MaxLengthConsts.CouponCode)
            .IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

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

        builder.Property(x => x.MaxUsageCount).IsRequired();
        builder.Property(x => x.MaxUsagePerUser).IsRequired();
        builder.Property(x => x.DurationDays).IsRequired().HasDefaultValue(30);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(MaxLengthConsts.CouponDescription);
        builder.Property(x => x.TotalUsedCount).IsRequired().HasDefaultValue(0);
    }
}