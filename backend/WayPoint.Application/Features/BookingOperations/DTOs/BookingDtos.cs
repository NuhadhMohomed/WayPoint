using WayPoint.Domain.Enums;

namespace WayPoint.Application.Features.BookingOperations.DTOs;

public class HoldSeatRequestDto
{
    public Guid ServiceId { get; set; }
    public Guid SeatId { get; set; }
}

public class HoldSeatResponseDto
{
    public Guid SeatHoldId { get; set; }
    public Guid ServiceId { get; set; }
    public Guid SeatId { get; set; }
    public DateTime HeldAt { get; set; }
    public DateTime HeldUntil { get; set; }
    public int RemainingSeconds { get; set; }
}

public class ConfirmPaymentRequestDto
{
    public Guid SeatHoldId { get; set; }
    public string PaymentMethod { get; set; } = "SandboxCard";
    public string TestCardNumber { get; set; } = "4000000000000001"; // Default success card
}

public class BookingConfirmationDto
{
    public Guid BookingId { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public string ServiceCode { get; set; } = string.Empty;
    public string SeatNumbers { get; set; } = string.Empty;
    public decimal TotalFareAmount { get; set; }
    public BookingStatus Status { get; set; }
    public TicketDto? Ticket { get; set; }
}

public class TicketDto
{
    public Guid TicketId { get; set; }
    public string QrCodePayload { get; set; } = string.Empty;
    public TicketStatus Status { get; set; }
    public bool IsBoarded { get; set; }
}

public class CancelBookingRequestDto
{
    public Guid BookingId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class RefundCalculationDto
{
    public Guid BookingId { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal RefundPercentage { get; set; }
    public decimal RefundAmount { get; set; }
    public string PolicyApplied { get; set; } = string.Empty;
}
