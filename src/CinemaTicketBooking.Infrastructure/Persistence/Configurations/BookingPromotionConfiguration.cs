using CinemaTicketBooking.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaTicketBooking.Infrastructure.Persistence.Configurations;

public class BookingPromotionConfiguration : IEntityTypeConfiguration<BookingPromotion>
{
    public void Configure(EntityTypeBuilder<BookingPromotion> builder)
    {
        builder.ToTable("booking_promotions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.BookingId).IsRequired();
        builder.Property(x => x.PromotionProgramId).IsRequired();
        builder.Property(x => x.PromotionName).HasMaxLength(MaxLengthConsts.Name).IsRequired();
        builder.Property(x => x.DiscountAmount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.DiscountType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.HasOne<Booking>()
            .WithMany()
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
