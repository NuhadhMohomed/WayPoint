using WayPoint.Application.DTOs.Booking;

namespace WayPoint.Application.Common.Interfaces;

public interface IBookingService
{
    // Seat Holds (US-PASS-003)
    Task<SeatHoldResponseDto> CreateSeatHoldAsync(SeatHoldRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> ReleaseSeatHoldAsync(Guid holdId, CancellationToken cancellationToken = default);
    Task<SeatHoldResponseDto?> GetSeatHoldStatusAsync(Guid holdId, CancellationToken cancellationToken = default);

    // Payment Sandbox (US-PASS-004)
    Task<PaymentChargeResponseDto> ProcessSandboxPaymentAsync(PaymentChargeRequestDto request, CancellationToken cancellationToken = default);

    // Checkout & Atomic Booking Confirmation (US-PASS-004)
    Task<BookingConfirmationDto> ExecuteCheckoutAsync(BookingCheckoutRequestDto request, CancellationToken cancellationToken = default);

    // Booking History & Management (US-PASS-005)
    Task<List<HistoricalBookingDto>> GetPassengerBookingsAsync(Guid? passengerId = null, CancellationToken cancellationToken = default);
    Task<HistoricalBookingDto?> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);
    Task<HistoricalBookingDto?> GetBookingByReferenceAsync(string bookingReference, CancellationToken cancellationToken = default);

    // Tiered Cancellations & Refunds (BR-REFUND-001)
    Task<RefundResponseDto> CancelBookingAndRefundAsync(CancelBookingRequestDto request, CancellationToken cancellationToken = default);

    // Digital QR Tickets & Conductor Verification
    Task<string> GenerateTicketPayloadAsync(string bookingReference, string serviceCode, string seatNumbers, string passengerName);
    Task<VerifyQrResponseDto> VerifyTicketQrAsync(VerifyQrRequestDto request, CancellationToken cancellationToken = default);
}
