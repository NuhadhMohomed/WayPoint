using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WayPoint.Application.Common.Interfaces;
using WayPoint.Application.DTOs.Booking;

namespace WayPoint.API.Controllers;

[ApiController]
[Route("api/v1/tickets")]
public class TicketController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly ILogger<TicketController> _logger;

    public TicketController(
        IBookingService bookingService,
        ILogger<TicketController> logger)
    {
        _bookingService = bookingService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a digital boarding pass ticket by ID with cryptographic HMAC payload.
    /// Enforces ownership verification: Booking.PassengerId must match authenticated user (Security Architecture §7.3).
    /// </summary>
    [Authorize(Policy = "RequirePassenger")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTicket(Guid id, CancellationToken cancellationToken)
    {
        var ticket = await _bookingService.GetTicketByIdAsync(id, cancellationToken);

        if (ticket == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Ticket Not Found",
                Detail = $"Ticket with ID '{id}' was not found."
            });
        }

        // BR-SECURITY: Verify ticket ownership (Security Architecture §7.3)
        // Booking.Passenger.UserId must match the authenticated user's JWT NameIdentifier claim
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdClaim, out var currentUserId))
        {
            if (ticket.PassengerUserId != currentUserId)
            {
                _logger.LogWarning(
                    "Ticket ownership violation: User {UserId} attempted to access ticket {TicketId}",
                    currentUserId, id);
                return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Access Denied",
                    Detail = "You do not have permission to view this ticket."
                });
            }
        }

        return Ok(new
        {
            ticketId = ticket.TicketId,
            bookingReference = ticket.BookingReference,
            serviceCode = ticket.ServiceCode,
            routeTitle = ticket.RouteTitle,
            seatNumbers = ticket.SeatNumbers,
            totalFare = ticket.TotalFare,
            status = ticket.Status,
            isBoarded = ticket.IsBoarded,
            boardedAt = ticket.BoardedAt,
            qrCodePayload = ticket.QrCodePayload,
            issuedAt = ticket.IssuedAt
        });
    }

    /// <summary>
    /// Conductor boarding validation endpoint. Cryptographically verifies HMAC signature and records boarding.
    /// </summary>
    [Authorize(Policy = "RequireOperator")]
    [HttpPost("verify")]
    public async Task<ActionResult<VerifyQrResponseDto>> VerifyQr(
        [FromBody] VerifyQrRequestDto request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Conductor verifying QR ticket pass payload...");
        var result = await _bookingService.VerifyTicketQrAsync(request, cancellationToken);

        if (!result.IsValid)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
