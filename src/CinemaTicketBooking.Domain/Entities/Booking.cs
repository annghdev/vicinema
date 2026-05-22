using System.Text.Json.Serialization;

namespace CinemaTicketBooking.Domain;

/// <summary>
/// Booking represents a customer's reservation for a specific ShowTime.
/// It aggregates tickets and concessions, tracks pricing (original and discounted),
/// and enforces state transitions: Pending → Confirmed → CheckedIn, or → Cancelled.
/// </summary>
public class Booking : AggregateRoot
{
    public Guid ShowTimeId { get; set; }
    public ShowTime? ShowTime { get; set; }
    public Guid? CustomerId { get; set; }
    [JsonIgnore]
    public Customer? Customer { get; set; }
    public required string CustomerName { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Total price before any discount is applied.
    /// </summary>
    public decimal OriginAmount { get; set; }

    /// <summary>
    /// Final price after discount. Calculated by UpdateFinalAmount().
    /// </summary>
    public decimal FinalAmount { get; set; }
    public List<BookingTicket> Tickets { get; set; } = [];
    public List<BookingConcession> Concessions { get; set; } = [];
    public string? QrCode { get; set; }
    public BookingStatus Status { get; set; }

    /// <summary>
    /// Coupon code applied to this booking (null if none).
    /// </summary>
    public string? CouponCode { get; set; }

    /// <summary>
    /// Discount amount contributed by the coupon (part of the total discount).
    /// </summary>
    public decimal CouponDiscountAmount { get; set; }

    // =============================================================
    // Factory and data mutation
    // =============================================================

    /// <summary>
    /// Creates a new booking and raises a creation event.
    /// </summary>
    public static Booking Create(
        Guid showTimeId,
        Guid? customerId,
        string customerName,
        string phoneNumber,
        string email,
        BookingStatus status = BookingStatus.Pending)
    {
        var booking = new Booking
        {
            ShowTimeId = showTimeId,
            CustomerId = customerId,
            CustomerName = customerName,
            PhoneNumber = phoneNumber,
            Email = email,
            Status = status
        };

        booking.RaiseEvent(new BookingCreated(
            BookingId: booking.Id,
            ShowTimeId: booking.ShowTimeId,
            CustomerId: booking.CustomerId,
            CustomerName: booking.CustomerName,
            Email: booking.Email,
            PhoneNumber: booking.PhoneNumber,
            Status: booking.Status));

        return booking;
    }

    // =============================================================
    // Add Items: Tickets and Concessions
    // =============================================================

    /// <summary>
    /// Adds a locked ticket to this booking.
    /// The ticket must belong to the same ShowTime and be locked by the current Customer.
    /// </summary>
    public void AddTicket(Ticket ticket)
    {
        // 1. Validate customer exists
        if (Customer == null)
        {
            throw new InvalidOperationException("Customer information is required to add a ticket to the booking.");
        }

        // 2. Validate ticket belongs to the same ShowTime
        if (ticket.ShowTimeId != ShowTimeId)
        {
            throw new InvalidOperationException("Ticket does not belong to the same showtime as the booking.");
        }

        // 3. Validate ticket is in Locking status
        if (ticket.Status != TicketStatus.Locking)
        {
            throw new InvalidOperationException("Ticket is not available for booking.");
        }

        // 4. Validate the ticket is locked by this customer (by SessionId or CustomerId)
        if (!(ticket.LockingBy == Customer.SessionId || ticket.LockingBy == Customer.Id.ToString()))
        {
            throw new InvalidOperationException("Ticket is not available for booking.");
        }

        // 5. Create BookingTicket join entity and accumulate price
        Tickets.Add(new BookingTicket
        {
            Id = Guid.CreateVersion7(),
            BookingId = Id,
            TicketId = ticket.Id,
            Ticket = ticket
        });

        OriginAmount += ticket.Price;
    }

    /// <summary>
    /// Adds a concession item (snack/drink) to this booking with the specified quantity.
    /// </summary>
    public void AddConcession(Concession concession, int quantity)
    {
        // 1. Validate concession is available
        if (!concession.IsAvailable)
        {
            throw new InvalidOperationException("Concession is not available for booking.");
        }

        // 2. Validate quantity is positive
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        }

        // 3. Create BookingConcession join entity and accumulate price
        Concessions.Add(new BookingConcession
        {
            Id = Guid.CreateVersion7(),
            BookingId = Id,
            ConcessionId = concession.Id,
            Concession = concession,
            Quantity = quantity
        });
        OriginAmount += concession.Price * quantity;
    }

    // =============================================================
    // State Transitions: Confirm, Cancel, CheckIn
    // =============================================================

    /// <summary>
    /// Confirms the booking and marks all associated tickets as Sold.
    /// Tickets must already be in PendingPayment state.
    /// Only Pending bookings can be confirmed.
    /// </summary>
    public void Confirm()
    {
        if (Status != BookingStatus.Pending)
        {
            throw new InvalidOperationException("Only pending bookings can be confirmed.");
        }
        Status = BookingStatus.Confirmed;

        // Mark each ticket as sold, linking it to this booking
        foreach (var bookingTicket in Tickets)
        {
            if (bookingTicket.Ticket == null)
            {
                throw new InvalidOperationException("Booking ticket must have an associated ticket.");
            }
            bookingTicket.Ticket.MarkAsSold(Id);
        }

        RaiseEvent(new BookingConfirmed(
            BookingId: Id,
            ShowTimeId: ShowTimeId,
            CustomerId: CustomerId,
            CustomerName: CustomerName,
            Email: Email,
            PhoneNumber: PhoneNumber,
            FinalAmount: FinalAmount,
            TicketCount: Tickets.Count,
            ShowTimeStartAt: ShowTime?.StartAt ?? DateTimeOffset.MinValue));
    }

    /// <summary>
    /// Cancels the booking and releases all associated tickets back to Available.
    /// Cannot cancel already-cancelled or checked-in bookings.
    /// </summary>
    public void Cancel()
    {
        if (Status == BookingStatus.Cancelled)
        {
            throw new InvalidOperationException("Booking is already cancelled.");
        }

        if(Status == BookingStatus.CheckedIn)
        {
            throw new InvalidOperationException("Checked-in bookings cannot be cancelled.");
        }

        var previousStatus = Status;
        Status = BookingStatus.Cancelled;

        // Release each ticket back to the available pool
        var releasedTicketIds = new List<Guid>();
        foreach (var bookingTicket in Tickets)
        {
            if (bookingTicket.Ticket == null)
            {
                throw new InvalidOperationException("Booking ticket must have an associated ticket.");
            }
            bookingTicket.Ticket.Release();
            releasedTicketIds.Add(bookingTicket.TicketId);
        }

        RaiseEvent(new BookingCancelled(
            BookingId: Id,
            ShowTimeId: ShowTimeId,
            CustomerId: CustomerId,
            CustomerName: CustomerName,
            Email: Email,
            PhoneNumber: PhoneNumber,
            FinalAmount: FinalAmount,
            PreviousStatus: previousStatus,
            ReleasedTicketIds: releasedTicketIds));
    }

    /// <summary>
    /// Marks the booking as checked-in (customer arrived at the cinema).
    /// Only Confirmed bookings can be checked in.
    /// </summary>
    public void CheckIn()
    {
        if (Status != BookingStatus.Confirmed)
        {
            throw new InvalidOperationException("Only confirmed bookings can be checked in.");
        }
        Status = BookingStatus.CheckedIn;

        RaiseEvent(new BookingCheckedIn(
            BookingId: Id,
            ShowTimeId: ShowTimeId,
            CustomerId: CustomerId,
            CustomerName: CustomerName,
            TicketCount: Tickets.Count,
            FinalAmount: FinalAmount));
    }

    // =============================================================
    // Coupon
    // =============================================================

    /// <summary>
    /// Applies a coupon discount to the booking.
    /// Records the coupon code and discount amount.
    /// Does NOT modify FinalAmount — call <see cref="UpdateFinalAmount"/> after this.
    /// </summary>
    public void ApplyCoupon(string couponCode, decimal discountAmount)
    {
        if (string.IsNullOrWhiteSpace(couponCode))
            throw new ArgumentException("Coupon code cannot be empty.", nameof(couponCode));

        if (discountAmount < 0)
            throw new ArgumentException("Coupon discount amount cannot be negative.", nameof(discountAmount));

        CouponCode = couponCode;
        CouponDiscountAmount = discountAmount;

        RaiseEvent(new CouponApplied(
            BookingId: Id,
            CustomerId: CustomerId,
            CustomerCouponId: null,
            CouponCode: couponCode,
            DiscountAmount: discountAmount));
    }

    // =============================================================
    // Pricing: Apply discount to calculate final amount
    // =============================================================

    /// <summary>
    /// Calculates the final amount after applying loyalty and coupon discounts.
    /// FinalAmount = max(0, OriginAmount - loyaltyDiscount - couponDiscount).
    /// </summary>
    public void UpdateFinalAmount(decimal loyaltyDiscount, decimal couponDiscount)
    {
        if (loyaltyDiscount < 0)
            throw new ArgumentException("Loyalty discount cannot be negative.", nameof(loyaltyDiscount));
        if (couponDiscount < 0)
            throw new ArgumentException("Coupon discount cannot be negative.", nameof(couponDiscount));

        CouponDiscountAmount = couponDiscount;
        FinalAmount = Math.Max(0, OriginAmount - loyaltyDiscount - couponDiscount);
    }

    /// <summary>
    /// Legacy overload for backward compatibility (loyalty-only discount).
    /// </summary>
    public void UpdateFinalAmount(decimal discountAmount)
    {
        UpdateFinalAmount(discountAmount, CouponDiscountAmount);
    }
}

/// <summary>
/// Join entity between Booking and Ticket (many-to-many).
/// </summary>
public class BookingTicket : IEntity
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public Guid TicketId { get; set; }
    public Ticket? Ticket { get; set; }
}

/// <summary>
/// Join entity between Booking and Concession (many-to-many with quantity).
/// </summary>
public class BookingConcession : IEntity
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public Guid ConcessionId { get; set; }
    public Concession? Concession { get; set; }
    public int Quantity { get; set; }
}

//public enum BookingStatus
//{
//    Initializing,
//    Pending,
//    Confirmed,
//    CheckedIn,
//    Cancelled,
//}
