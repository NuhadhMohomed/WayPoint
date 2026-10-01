using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WayPoint.Application.Common.Interfaces;
using WayPoint.Application.DTOs.Booking;

namespace WayPoint.API.Controllers;

[ApiController]
[Route("api/v1/tickets")]
public class TicketController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly IWayPointDbContext _context;
    private readonly ILogger<TicketController> _logger;

    public TicketController(
        IBookingService bookingService,
        IWayPointDbContext context,
        ILogger<TicketController> logger)
    {
        _bookingService = bookingService;
        _context = context;
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
        var ticket = await _context.Tickets
            .Include(t => t.Booking)
                .ThenInclude(b => b.Passenger)
            .Include(t => t.Booking)
                .ThenInclude(b => b.Service)
                    .ThenInclude(s => s.Route)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

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
            if (ticket.Booking.Passenger?.UserId != currentUserId)
            {
                _logger.LogWarning(
                    "Ticket ownership violation: User {UserId} attempted to access ticket {TicketId} owned by passenger profile {PassengerId}",
                    currentUserId, id, ticket.Booking.PassengerId);
                return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Access Denied",
                    Detail = "You do not have permission to view this ticket."
                });
            }
        }

        var route = ticket.Booking.Service?.Route;
        return Ok(new
        {
            ticketId = ticket.Id,
            bookingReference = ticket.Booking.BookingReference,
            serviceCode = ticket.Booking.Service?.ServiceCode,
            routeTitle = route != null ? $"{route.OriginCity} - {route.DestinationCity}" : "Intercity Express",
            seatNumbers = ticket.Booking.SeatNumbers.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            totalFare = ticket.Booking.TotalFareAmount,
            status = ticket.Status.ToString(),
            isBoarded = ticket.IsBoarded,
            boardedAt = ticket.BoardedAt,
            qrCodePayload = ticket.QrCodePayload,
            issuedAt = ticket.CreatedAt
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
