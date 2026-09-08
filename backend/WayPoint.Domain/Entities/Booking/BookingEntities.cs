using WayPoint.Domain.Common;
using WayPoint.Domain.Entities.Fleet;
using WayPoint.Domain.Entities.Identity;
using WayPoint.Domain.Entities.Journey;
using WayPoint.Domain.Enums;

namespace WayPoint.Domain.Entities.Booking;

public class SeatHold : BaseEntity
{
    public Guid ServiceId { get; set; }
    public Guid SeatId { get; set; }
    public Guid PassengerId { get; set; }
    public DateTime HeldAt { get; set; } = DateTime.UtcNow;
    public DateTime HeldUntil { get; set; }
    public SeatHoldStatus Status { get; set; } = SeatHoldStatus.Held;

    // Navigation properties
    public Service Service { get; set; } = null!;
    public Seat Seat { get; set; } = null!;
    public PassengerProfile Passenger { get; set; } = null!;
}

public class Booking : BaseEntity
{
    public string BookingReference { get; set; } = string.Empty;
    public Guid PassengerId { get; set; }
    public Guid ServiceId { get; set; }
    public string SeatNumbers { get; set; } = string.Empty;
    public decimal TotalFareAmount { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Confirmed;

    // Navigation properties
    public PassengerProfile Passenger { get; set; } = null!;
    public Service Service { get; set; } = null!;
    public Ticket? Ticket { get; set; }
    public ICollection<PaymentAttempt> PaymentAttempts { get; set; } = new List<PaymentAttempt>();
    public ICollection<Refund> Refunds { get; set; } = new List<Refund>();
}

public class Ticket : BaseEntity
{
    public Guid BookingId { get; set; }
    public string QrCodePayload { get; set; } = string.Empty;
    public TicketStatus Status { get; set; } = TicketStatus.Issued;
    public bool IsBoarded { get; set; } = false;
    public DateTime? BoardedAt { get; set; }

    // Navigation properties
    public Booking Booking { get; set; } = null!;
}

public class PaymentAttempt : BaseEntity
{
    public Guid BookingId { get; set; }
    public string GatewayTransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Booking Booking { get; set; } = null!;
}

public class Refund : BaseEntity
{
    public Guid BookingId { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal Percentage { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Booking Booking { get; set; } = null!;
}
