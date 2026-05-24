using CinemaTicketBooking.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaTicketBooking.Infrastructure.Persistence.Configurations;

public class PromotionConditionConfiguration : IEntityTypeConfiguration<PromotionCondition>
{
    public void Configure(EntityTypeBuilder<PromotionCondition> builder)
    {
        builder.ToTable("promotion_conditions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.ConditionType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Value).HasMaxLength(MaxLengthConsts.Name);
        builder.Property(x => x.SecondaryValue).HasMaxLength(MaxLengthConsts.Name);
    }
}
