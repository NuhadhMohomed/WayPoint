namespace WayPoint.Application.DTOs.Booking;

public class ServiceSummaryDto
{
    public Guid Id { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public string RouteName { get; set; } = string.Empty;
    public string OriginCity { get; set; } = string.Empty;
    public string DestinationCity { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public decimal BaseFare { get; set; }
    public string BusRegistration { get; set; } = string.Empty;
    public string BusClass { get; set; } = string.Empty;
    public int TotalSeats { get; set; }
}

public class SeatHoldRequestDto
{
    public Guid ServiceId { get; set; }
    public List<string> SeatNumbers { get; set; } = new();
    public Guid? PassengerId { get; set; }
    public string? PassengerName { get; set; }
}

public class SeatHoldResponseDto
{
    public Guid HoldId { get; set; }
    public Guid ServiceId { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public string RouteTitle { get; set; } = string.Empty;
    public List<string> SeatNumbers { get; set; } = new();
    public DateTime HeldAt { get; set; }
    public DateTime HeldUntil { get; set; }
    public int SecondsRemaining { get; set; }
    public decimal BaseFarePerSeat { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Held";
}

public class PaymentChargeRequestDto
{
    public Guid? HoldId { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public string CardholderName { get; set; } = string.Empty;
    public string ExpiryDate { get; set; } = string.Empty;
    public string Cvv { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class PaymentChargeResponseDto
{
    public bool IsSuccess { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string GatewayStatus { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

public class BookingCheckoutRequestDto
{
    public Guid HoldId { get; set; }
    public string PaymentTransactionId { get; set; } = string.Empty;
    public string? PassengerName { get; set; }
    public string? PassengerEmail { get; set; }
    public string? PassengerPhone { get; set; }
}

public class BookingConfirmationDto
{
    public Guid BookingId { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public Guid ServiceId { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public string RouteTitle { get; set; } = string.Empty;
    public string OriginCity { get; set; } = string.Empty;
    public string DestinationCity { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public List<string> SeatNumbers { get; set; } = new();
    public decimal TotalFareAmount { get; set; }
    public string Status { get; set; } = "Confirmed";
    public DateTime ConfirmedAt { get; set; }
    public Guid TicketId { get; set; }
    public string QrCodePayload { get; set; } = string.Empty;
}

public class HistoricalBookingDto
{
    public Guid BookingId { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public Guid ServiceId { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public string RouteTitle { get; set; } = string.Empty;
    public string OriginCity { get; set; } = string.Empty;
    public string DestinationCity { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public List<string> SeatNumbers { get; set; } = new();
    public decimal TotalFareAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime BookedAt { get; set; }
    public Guid? TicketId { get; set; }
    public string? QrCodePayload { get; set; }
    public bool IsBoarded { get; set; }
    public decimal? RefundAmount { get; set; }
    public decimal? RefundPercentage { get; set; }
    public string? CancellationReason { get; set; }
    public int HoursUntilDeparture { get; set; }
    public decimal EligibleRefundPercentage { get; set; }
}

public class CancelBookingRequestDto
{
    public string BookingReference { get; set; } = string.Empty;
    public string Reason { get; set; } = "Change of travel plans";
}

public class RefundResponseDto
{
    public string BookingReference { get; set; } = string.Empty;
    public decimal OriginalFarePaid { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal RefundPercentage { get; set; }
    public decimal CancellationFee { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Cancelled";
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

public class VerifyQrRequestDto
{
    public string QrCodePayload { get; set; } = string.Empty;
}

public class VerifyQrResponseDto
{
    public bool IsValid { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? BookingReference { get; set; }
    public string? ServiceCode { get; set; }
    public string? RouteTitle { get; set; }
    public string? PassengerName { get; set; }
    public List<string> SeatNumbers { get; set; } = new();
    public bool IsBoarded { get; set; }
    public DateTime? BoardedAt { get; set; }
    public string Message { get; set; } = string.Empty;
}
