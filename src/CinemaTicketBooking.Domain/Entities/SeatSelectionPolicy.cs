namespace CinemaTicketBooking.Domain;

/// <summary>
/// Stores global policy settings for pre-checkout seat selection validation.
/// </summary>
public class SeatSelectionPolicy : AggregateRoot
{
    public required string Name { get; set; }
    public bool IsGlobalDefault { get; set; } = true;
    public bool IsActive { get; set; } = true;

    public int MaxTicketsPerCheckout { get; set; } = 8;
    public int MaxRowsPerCheckout { get; set; } = 2;

    public SeatSelectionPolicyLevel OrphanSeatLevel { get; set; } = SeatSelectionPolicyLevel.Block;
    public SeatSelectionPolicyLevel CheckerboardLevel { get; set; } = SeatSelectionPolicyLevel.Block;
    public SeatSelectionPolicyLevel SplitAcrossAisleLevel { get; set; } = SeatSelectionPolicyLevel.Block;
    public SeatSelectionPolicyLevel IsolatedRowEndSingleLevel { get; set; } = SeatSelectionPolicyLevel.Warning;
    public SeatSelectionPolicyLevel MisalignedRowsLevel { get; set; } = SeatSelectionPolicyLevel.Block;

    /// <summary>
    /// Creates the default global policy used by pre-checkout validation.
    /// </summary>
    public static SeatSelectionPolicy CreateDefault()
    {
        var policy = new SeatSelectionPolicy
        {
            Name = "Global default pre-checkout seat policy",
            IsGlobalDefault = true,
            IsActive = true,
            MaxTicketsPerCheckout = 8,
            MaxRowsPerCheckout = 2,
            OrphanSeatLevel = SeatSelectionPolicyLevel.Block,
            CheckerboardLevel = SeatSelectionPolicyLevel.Block,
            SplitAcrossAisleLevel = SeatSelectionPolicyLevel.Block,
            IsolatedRowEndSingleLevel = SeatSelectionPolicyLevel.Warning,
            MisalignedRowsLevel = SeatSelectionPolicyLevel.Block
        };

        policy.RaiseEvent(new SeatSelectionPolicyCreated(policy.Id));
        return policy;
    }

    public void Update(
        string name,
        bool isGlobalDefault,
        bool isActive,
        int maxTickets,
        int maxRows,
        SeatSelectionPolicyLevel orphan,
        SeatSelectionPolicyLevel checkerboard,
        SeatSelectionPolicyLevel split,
        SeatSelectionPolicyLevel isolated,
        SeatSelectionPolicyLevel misaligned)
    {
        Name = name;
        IsGlobalDefault = isGlobalDefault;
        IsActive = isActive;
        MaxTicketsPerCheckout = maxTickets;
        MaxRowsPerCheckout = maxRows;
        OrphanSeatLevel = orphan;
        CheckerboardLevel = checkerboard;
        SplitAcrossAisleLevel = split;
        IsolatedRowEndSingleLevel = isolated;
        MisalignedRowsLevel = misaligned;

        RaiseEvent(new SeatSelectionPolicyUpdated(Id));
    }

    /// <summary>
    /// Resolves configured policy level for a violation type.
    /// </summary>
    public SeatSelectionPolicyLevel ResolveLevel(SeatSelectionViolationType violationType)
    {
        return violationType switch
        {
            SeatSelectionViolationType.OrphanSeat => OrphanSeatLevel,
            SeatSelectionViolationType.Checkerboard => CheckerboardLevel,
            SeatSelectionViolationType.SplitAcrossAisle => SplitAcrossAisleLevel,
            SeatSelectionViolationType.IsolatedRowEndSingle => IsolatedRowEndSingleLevel,
            SeatSelectionViolationType.MisalignedRows => MisalignedRowsLevel,
            SeatSelectionViolationType.TicketUnavailable => SeatSelectionPolicyLevel.Block,
            SeatSelectionViolationType.MaxTickets => SeatSelectionPolicyLevel.Block,
            SeatSelectionViolationType.MaxRows => SeatSelectionPolicyLevel.Block,
            _ => SeatSelectionPolicyLevel.Block
        };
    }
}
