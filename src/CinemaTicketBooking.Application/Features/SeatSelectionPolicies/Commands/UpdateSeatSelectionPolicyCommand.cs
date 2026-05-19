using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Domain;
using FluentValidation;

namespace CinemaTicketBooking.Application.Features;

/// <summary>
/// Updates an existing seat selection policy.
/// </summary>
public class UpdateSeatSelectionPolicyCommand : ICommand
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public bool IsGlobalDefault { get; set; }
    public bool IsActive { get; set; }
    public int MaxTicketsPerCheckout { get; set; }
    public int MaxRowsPerCheckout { get; set; }
    public SeatSelectionPolicyLevel OrphanSeatLevel { get; set; }
    public SeatSelectionPolicyLevel CheckerboardLevel { get; set; }
    public SeatSelectionPolicyLevel SplitAcrossAisleLevel { get; set; }
    public SeatSelectionPolicyLevel IsolatedRowEndSingleLevel { get; set; }
    public SeatSelectionPolicyLevel MisalignedRowsLevel { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}

/// <summary>
/// Handles update-seat-selection-policy requests.
/// </summary>
public class UpdateSeatSelectionPolicyHandler(IUnitOfWork uow)
{
    /// <summary>
    /// Updates an existing seat selection policy and persists changes.
    /// </summary>
    public async Task Handle(UpdateSeatSelectionPolicyCommand cmd, CancellationToken ct)
    {
        var policy = await uow.SeatSelectionPolicies.GetByIdAsync(cmd.Id, ct);
        if (policy is null)
        {
            throw new InvalidOperationException($"Seat selection policy with ID '{cmd.Id}' not found.");
        }

        policy.Update(
            cmd.Name,
            cmd.IsGlobalDefault,
            cmd.IsActive,
            cmd.MaxTicketsPerCheckout,
            cmd.MaxRowsPerCheckout,
            cmd.OrphanSeatLevel,
            cmd.CheckerboardLevel,
            cmd.SplitAcrossAisleLevel,
            cmd.IsolatedRowEndSingleLevel,
            cmd.MisalignedRowsLevel);

        uow.SeatSelectionPolicies.Update(policy);
        await uow.CommitAsync(ct);
    }
}

/// <summary>
/// Validates update-seat-selection-policy command payload.
/// </summary>
public class UpdateSeatSelectionPolicyValidator : AbstractValidator<UpdateSeatSelectionPolicyCommand>
{
    public UpdateSeatSelectionPolicyValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("ID is required.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255).WithMessage("Name must not exceed 255 characters.");
        RuleFor(x => x.MaxTicketsPerCheckout).GreaterThan(0).WithMessage("Max tickets must be positive.");
        RuleFor(x => x.MaxRowsPerCheckout).GreaterThan(0).WithMessage("Max rows must be positive.");
        RuleFor(x => x.OrphanSeatLevel).IsInEnum();
        RuleFor(x => x.CheckerboardLevel).IsInEnum();
        RuleFor(x => x.SplitAcrossAisleLevel).IsInEnum();
        RuleFor(x => x.IsolatedRowEndSingleLevel).IsInEnum();
        RuleFor(x => x.MisalignedRowsLevel).IsInEnum();
    }
}
