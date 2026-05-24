using CinemaTicketBooking.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaTicketBooking.Infrastructure.Persistence.Configurations;

public class BookingConcessionConfiguration : IEntityTypeConfiguration<BookingConcession>
{
    public void Configure(EntityTypeBuilder<BookingConcession> builder)
    {
        builder.ToTable("booking_concessions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.BookingId).IsRequired();
        builder.Property(x => x.ConcessionId).IsRequired();
        builder.Property(x => x.Quantity).IsRequired();
        builder.Property(x => x.IsFree).IsRequired();

        builder.HasOne(x => x.Concession)
            .WithMany()
            .HasForeignKey(x => x.ConcessionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
