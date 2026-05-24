using CinemaTicketBooking.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaTicketBooking.Infrastructure.Persistence.Configurations;

public class PromotionFreeConcessionItemConfiguration : IEntityTypeConfiguration<PromotionFreeConcessionItem>
{
    public void Configure(EntityTypeBuilder<PromotionFreeConcessionItem> builder)
    {
        builder.ToTable("promotion_free_concession_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.ConcessionId).IsRequired();
        builder.Property(x => x.Quantity).IsRequired();
    }
}
