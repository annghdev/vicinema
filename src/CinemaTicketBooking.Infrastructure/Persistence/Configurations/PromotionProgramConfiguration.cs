using CinemaTicketBooking.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaTicketBooking.Infrastructure.Persistence.Configurations;

public class PromotionProgramConfiguration : IEntityTypeConfiguration<PromotionProgram>
{
    public void Configure(EntityTypeBuilder<PromotionProgram> builder)
    {
        builder.ToTable("promotion_programs");
        builder.ConfigureAggregateRoot();

        builder.Property(x => x.Name)
            .HasMaxLength(MaxLengthConsts.Name)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(MaxLengthConsts.CouponDescription);

        builder.Property(x => x.PosterImage)
            .HasMaxLength(MaxLengthConsts.Url);

        builder.Property(x => x.StartDate).IsRequired();
        builder.Property(x => x.EndDate).IsRequired();

        builder.Property(x => x.IsActive).IsRequired();

        builder.Property(x => x.DiscountType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.DiscountForm)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(x => x.DiscountValue)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.MaxDiscountAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.MaxDiscountPercentage)
            .HasColumnType("decimal(5,2)");

        builder.Property(x => x.MaxUsagePerCustomer);

        builder.HasMany(p => p.Conditions)
            .WithOne()
            .HasForeignKey("PromotionProgramId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.FreeConcessionItems)
            .WithOne()
            .HasForeignKey("PromotionProgramId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
