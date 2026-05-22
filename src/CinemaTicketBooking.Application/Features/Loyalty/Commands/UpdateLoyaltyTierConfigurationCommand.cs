using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Domain;
using CinemaTicketBooking.Domain.Services;
using FluentValidation;

namespace CinemaTicketBooking.Application.Features.Loyalty;

/// <summary>
/// Admin updates an existing loyalty tier configuration.
/// Tiers are seeded once; only updates are allowed.
/// </summary>
public class UpdateLoyaltyTierConfigurationCommand : ICommand
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MinPoints { get; set; }
    public int? MaxPoints { get; set; }
    public decimal TicketDiscountPercent { get; set; }
    public decimal ConcessionDiscountPercent { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}

/// <summary>
/// Handles loyalty tier configuration update requests.
/// </summary>
public class UpdateLoyaltyTierConfigurationHandler(IUnitOfWork uow)
{
    /// <summary>
    /// Updates the specified tier configuration and persists.
    /// </summary>
    public async Task Handle(UpdateLoyaltyTierConfigurationCommand cmd, CancellationToken ct)
    {
        var config = await uow.LoyaltyTiers.GetByIdAsync(cmd.Id, ct);
        if (config is null)
        {
            throw new InvalidOperationException($"Loyalty tier configuration with ID '{cmd.Id}' not found.");
        }

        config.UpdateConfig(
            name: cmd.Name,
            minPoints: cmd.MinPoints,
            maxPoints: cmd.MaxPoints,
            ticketDiscountPercent: cmd.TicketDiscountPercent,
            concessionDiscountPercent: cmd.ConcessionDiscountPercent,
            description: cmd.Description,
            isActive: cmd.IsActive);

        uow.LoyaltyTiers.Update(config);
        await uow.CommitAsync(ct);
    }
}

/// <summary>
/// Validates the update loyalty tier configuration command.
/// </summary>
public class UpdateLoyaltyTierConfigurationValidator : AbstractValidator<UpdateLoyaltyTierConfigurationCommand>
{
    public UpdateLoyaltyTierConfigurationValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("ID is required.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(MaxLengthConsts.Name);
        RuleFor(x => x.MinPoints).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TicketDiscountPercent).InclusiveBetween(0m, 100m);
        RuleFor(x => x.ConcessionDiscountPercent).InclusiveBetween(0m, 100m);
        RuleFor(x => x.Description).MaximumLength(MaxLengthConsts.Description);
    }
}
