using CinemaTicketBooking.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaTicketBooking.Infrastructure.Persistence.Configurations;

public class CustomerPromotionUsageConfiguration : IEntityTypeConfiguration<CustomerPromotionUsage>
{
    public void Configure(EntityTypeBuilder<CustomerPromotionUsage> builder)
    {
        builder.ToTable("customer_promotion_usages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.CustomerId).IsRequired();
        builder.Property(x => x.PromotionProgramId).IsRequired();
        builder.Property(x => x.BookingId).IsRequired();
        builder.Property(x => x.UsedAt).IsRequired();

        builder.HasIndex(u => new { u.CustomerId, u.PromotionProgramId });
    }
}
