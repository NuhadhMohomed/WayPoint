using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WayPoint.Application.Common.Interfaces;
using WayPoint.Application.DTOs.Booking;

namespace WayPoint.API.Controllers;

[ApiController]
[Route("api/v1/bookings/hold")]
[Authorize]
public class SeatHoldController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly IWayPointDbContext? _context;
    private readonly ICurrentUserService? _currentUserService;
    private readonly ILogger<SeatHoldController> _logger;

    public SeatHoldController(
        IBookingService bookingService,
        ILogger<SeatHoldController> logger,
        IWayPointDbContext? context = null,
        ICurrentUserService? currentUserService = null)
    {
        _bookingService = bookingService;
        _logger = logger;
        _context = context;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Attempts to temporarily hold seats for 10 minutes (US-PASS-003).
    /// Enforces BOLA ownership verification: Holds are tied to the authenticated passenger profile.
    /// Returns 409 Conflict if any seat is already held or booked.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SeatHoldResponseDto>> CreateSeatHold([FromBody] SeatHoldRequestDto request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var isStaff = IsCallerStaff();

        if (userId.HasValue && !isStaff && _context != null)
        {
            var passengerProfile = await _context.PassengerProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId.Value, cancellationToken);

            if (passengerProfile == null)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Passenger Profile Required",
                    Detail = "Authenticated user does not have a valid passenger profile."
                });
            }

            // Spoofing prevention: Ensure client didn't supply another passenger's ID
            if (request.PassengerId.HasValue &&
                request.PassengerId.Value != passengerProfile.Id &&
                request.PassengerId.Value != passengerProfile.UserId)
            {
                _logger.LogWarning(
                    "Seat hold ownership violation: User {UserId} attempted to create hold for passenger {PassengerId}",
                    userId.Value, request.PassengerId.Value);
                return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Access Denied",
                    Detail = "You cannot create a seat hold for another passenger."
                });
            }

            // Always enforce trusted server-side passenger profile ID for non-staff
            request.PassengerId = passengerProfile.Id;
        }

        try
        {
            var result = await _bookingService.CreateSeatHoldAsync(request, cancellationToken);
            return Ok(result);
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
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = ex.Message
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Service Not Found",
                Detail = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Seat hold conflict: {Message}", ex.Message);
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Seat Hold Conflict",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Releases an active seat hold before expiration upon passenger cancellation or abandonment.
    /// Enforces BOLA ownership verification: Passengers can only release holds they own.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> ReleaseSeatHold(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var isStaff = IsCallerStaff();

        if (userId.HasValue && !isStaff && _context != null)
        {
            var hold = await _context.SeatHolds
                .Include(h => h.Passenger)
                .FirstOrDefaultAsync(h => h.ReservationGroupId == id || h.Id == id, cancellationToken);

            if (hold == null)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Seat Hold Not Found",
                    Detail = $"No active seat hold found with ID '{id}'."
                });
            }

            if (hold.Passenger?.UserId != userId.Value)
            {
                _logger.LogWarning(
                    "Seat hold release ownership violation: User {UserId} attempted to release hold {HoldId}",
                    userId.Value, id);
                return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Access Denied",
                    Detail = "You do not have permission to release this seat hold."
                });
            }
        }

        try
        {
            var released = await _bookingService.ReleaseSeatHoldAsync(id, cancellationToken);
            if (!released)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Seat Hold Not Found",
                    Detail = $"No active seat hold found with ID '{id}'."
                });
            }

            return Ok(new { success = true, message = "Seat hold released successfully." });
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
    /// Inspects the status and remaining seconds of an active seat hold.
    /// Enforces BOLA ownership verification: Passengers can only inspect holds they own.
    /// </summary>
    [HttpGet("{id:guid}/status")]
    public async Task<ActionResult<SeatHoldResponseDto>> GetSeatHoldStatus(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var isStaff = IsCallerStaff();

        if (userId.HasValue && !isStaff && _context != null)
        {
            var hold = await _context.SeatHolds
                .Include(h => h.Passenger)
                .FirstOrDefaultAsync(h => h.ReservationGroupId == id || h.Id == id, cancellationToken);

            if (hold == null)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Seat Hold Not Found",
                    Detail = $"Seat hold '{id}' was not found."
                });
            }

            if (hold.Passenger?.UserId != userId.Value)
            {
                _logger.LogWarning(
                    "Seat hold status ownership violation: User {UserId} attempted to inspect hold {HoldId}",
                    userId.Value, id);
                return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Access Denied",
                    Detail = "You do not have permission to view this seat hold."
                });
            }
        }

        try
        {
            var hold = await _bookingService.GetSeatHoldStatusAsync(id, cancellationToken);
            if (hold == null)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Seat Hold Not Found",
                    Detail = $"Seat hold '{id}' was not found."
                });
            }

            return Ok(hold);
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

    private Guid? GetCurrentUserId()
    {
        if (_currentUserService?.UserId.HasValue == true)
        {
            return _currentUserService.UserId;
        }

        var idClaim = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idClaim, out var id) ? id : null;
    }

    private bool IsCallerStaff()
    {
        if (_currentUserService != null)
        {
            return _currentUserService.IsStaff;
        }

        return User?.IsInRole("Admin") == true ||
               User?.IsInRole("TransportManager") == true ||
               User?.IsInRole("Operator") == true;
    }
}
