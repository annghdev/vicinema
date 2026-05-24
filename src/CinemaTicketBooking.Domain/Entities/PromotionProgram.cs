using CinemaTicketBooking.Domain.Enums;
using CinemaTicketBooking.Domain.Events;

namespace CinemaTicketBooking.Domain;

public class PromotionProgram : AggregateRoot
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PosterImage { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public PromotionDiscountType DiscountType { get; set; }
    public DiscountForm? DiscountForm { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public decimal? MaxDiscountPercentage { get; set; }
    public int? MaxUsagePerCustomer { get; set; }
    public List<PromotionCondition> Conditions { get; set; } = [];
    public List<PromotionFreeConcessionItem>? FreeConcessionItems { get; set; }

    private PromotionProgram() { }

    public static PromotionProgram Create(
        string name,
        string? description,
        string? posterImage,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        PromotionDiscountType discountType,
        DiscountForm? discountForm,
        decimal discountValue,
        decimal? maxDiscountAmount,
        decimal? maxDiscountPercentage,
        int? maxUsagePerCustomer)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));

        if (name.Length > MaxLengthConsts.Name)
            throw new ArgumentException($"Name cannot exceed {MaxLengthConsts.Name} characters.", nameof(name));

        if (startDate >= endDate)
            throw new ArgumentException("Start date must be before end date.", nameof(startDate));

        if (discountValue < 0)
            throw new ArgumentException("Discount value cannot be negative.", nameof(discountValue));

        if (maxDiscountAmount.HasValue && maxDiscountAmount.Value <= 0)
            throw new ArgumentException("Max discount amount must be greater than zero.", nameof(maxDiscountAmount));

        if (maxDiscountPercentage.HasValue && (maxDiscountPercentage.Value <= 0 || maxDiscountPercentage.Value > 100))
            throw new ArgumentException("Max discount percentage must be between 0 and 100.", nameof(maxDiscountPercentage));

        var promotion = new PromotionProgram
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            Description = description,
            PosterImage = posterImage,
            StartDate = startDate,
            EndDate = endDate,
            DiscountType = discountType,
            DiscountForm = discountForm,
            DiscountValue = discountValue,
            MaxDiscountAmount = maxDiscountAmount,
            MaxDiscountPercentage = maxDiscountPercentage,
            MaxUsagePerCustomer = maxUsagePerCustomer,
            Conditions = [],
            FreeConcessionItems = discountType == PromotionDiscountType.FreeConcession ? [] : null
        };

        promotion.RaiseEvent(new PromotionCreated(promotion.Id));
        return promotion;
    }

    public void UpdateBasicInfo(
        string name,
        string? description,
        string? posterImage,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        PromotionDiscountType discountType,
        DiscountForm? discountForm,
        decimal discountValue,
        decimal? maxDiscountAmount,
        decimal? maxDiscountPercentage,
        int? maxUsagePerCustomer)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));

        if (name.Length > MaxLengthConsts.Name)
            throw new ArgumentException($"Name cannot exceed {MaxLengthConsts.Name} characters.", nameof(name));

        if (startDate >= endDate)
            throw new ArgumentException("Start date must be before end date.", nameof(startDate));

        if (!Enum.IsDefined(discountType))
            throw new ArgumentException("Invalid discount type.", nameof(discountType));

        if (discountValue < 0)
            throw new ArgumentException("Discount value cannot be negative.", nameof(discountValue));

        if (maxDiscountAmount.HasValue && maxDiscountAmount.Value <= 0)
            throw new ArgumentException("Max discount amount must be greater than zero.", nameof(maxDiscountAmount));

        if (maxDiscountPercentage.HasValue && (maxDiscountPercentage.Value <= 0 || maxDiscountPercentage.Value > 100))
            throw new ArgumentException("Max discount percentage must be between 0 and 100.", nameof(maxDiscountPercentage));

        Name = name;
        Description = description;
        PosterImage = posterImage;
        StartDate = startDate;
        EndDate = endDate;
        DiscountType = discountType;
        DiscountForm = discountForm;
        DiscountValue = discountValue;
        MaxDiscountAmount = maxDiscountAmount;
        MaxDiscountPercentage = maxDiscountPercentage;
        MaxUsagePerCustomer = maxUsagePerCustomer;

        if (discountType == PromotionDiscountType.FreeConcession && FreeConcessionItems is null)
            FreeConcessionItems = [];

        RaiseEvent(new PromotionUpdated(Id));
    }

    public void AddCondition(PromotionCondition condition)
    {
        Conditions.Add(condition);
    }

    public void RemoveCondition(Guid conditionId)
    {
        Conditions.RemoveAll(c => c.Id == conditionId);
    }

    public void AddFreeConcessionItem(PromotionFreeConcessionItem item)
    {
        FreeConcessionItems ??= [];
        FreeConcessionItems.Add(item);
    }

    public void RemoveFreeConcessionItem(Guid itemId)
    {
        FreeConcessionItems?.RemoveAll(i => i.Id == itemId);
    }

    public void Toggle()
    {
        IsActive = !IsActive;

        if (IsActive)
            RaiseEvent(new PromotionActivated(Id));
        else
            RaiseEvent(new PromotionDeactivated(Id));
    }

    public void SoftDelete()
    {
        DeletedAt = DateTimeOffset.UtcNow;
        RaiseEvent(new PromotionDeleted(Id));
    }
}
