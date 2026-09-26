using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WayPoint.Application.Common.Interfaces;
using WayPoint.Application.DTOs.Booking;
using WayPoint.Infrastructure.Data;

namespace WayPoint.API.Controllers;

[ApiController]
[Route("api/v1/tickets")]
public class TicketController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly WayPointDbContext _context;
    private readonly ILogger<TicketController> _logger;

    public TicketController(
        IBookingService bookingService,
        WayPointDbContext context,
        ILogger<TicketController> logger)
    {
        _bookingService = bookingService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a digital boarding pass ticket by ID with cryptographic HMAC payload.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTicket(Guid id, CancellationToken cancellationToken)
    {
        var ticket = await _context.Tickets
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
    [HttpPost("verify-qr")]
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
