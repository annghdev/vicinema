namespace CinemaTicketBooking.Domain;

public record SeatSelectionPolicyCreated(Guid PolicyId) : BaseDomainEvent;
public record SeatSelectionPolicyUpdated(Guid PolicyId) : BaseDomainEvent;
public record SeatSelectionPolicyDeleted(Guid PolicyId) : BaseDomainEvent;
