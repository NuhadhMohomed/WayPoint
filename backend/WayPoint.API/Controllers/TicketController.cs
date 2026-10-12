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
    /// Enforces BOLA ownership verification: Only the ticket owner (or authorized staff/conductors) may view the ticket.
    /// </summary>
    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTicket(Guid id, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var currentUserId))
        {
            return Unauthorized();
        }

        var isStaff = User.IsInRole("Admin") || User.IsInRole("TransportManager") ||
                      User.IsInRole("Operator") || User.IsInRole("Conductor");

        try
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
            if (!isStaff && ticket.PassengerUserId != currentUserId)
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
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Access Denied",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Conductor boarding validation endpoint. Cryptographically verifies HMAC signature and records boarding.
    /// Preserves legitimate conductor and operator workflows.
    /// </summary>
    [Authorize(Roles = "Operator,TransportManager,Admin,Conductor")]
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
