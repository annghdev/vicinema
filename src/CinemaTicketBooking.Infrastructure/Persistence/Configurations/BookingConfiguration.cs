using CinemaTicketBooking.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaTicketBooking.Infrastructure.Persistence.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings");
        builder.ConfigureAggregateRoot();

        builder.Property(x => x.CustomerName).HasMaxLength(MaxLengthConsts.Name).IsRequired();
        builder.Property(x => x.PhoneNumber).HasMaxLength(MaxLengthConsts.PhoneNumber);
        builder.Property(x => x.Email).HasMaxLength(MaxLengthConsts.Email);
        builder.Property(x => x.OriginAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.FinalAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.QrCode).HasMaxLength(MaxLengthConsts.QrCode);
        builder.Property(x => x.Status).IsRequired();

        builder.Property(x => x.CouponCode).HasMaxLength(MaxLengthConsts.CouponCode).IsRequired(false);
        builder.Property(x => x.CouponDiscountAmount).HasPrecision(18, 2).IsRequired().HasDefaultValue(0m);

        builder.HasOne(x => x.ShowTime)
            .WithMany()
            .HasForeignKey(x => x.ShowTimeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Tickets)
            .WithOne()
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Concessions)
            .WithOne()
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.AppliedPromotions)
            .WithOne()
            .HasForeignKey("BookingId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
