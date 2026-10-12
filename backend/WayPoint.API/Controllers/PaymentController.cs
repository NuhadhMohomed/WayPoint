using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WayPoint.Application.Common.Interfaces;
using WayPoint.Application.DTOs.Booking;

namespace WayPoint.API.Controllers;

[ApiController]
[Route("api/v1/payments")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly IWayPointDbContext? _context;
    private readonly ICurrentUserService? _currentUserService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        IBookingService bookingService,
        ILogger<PaymentController> logger,
        IWayPointDbContext? context = null,
        ICurrentUserService? currentUserService = null)
    {
        _bookingService = bookingService;
        _logger = logger;
        _context = context;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Processes a simulated payment card charge against the sandbox gateway (US-PASS-004).
    /// Enforces BOLA ownership verification: If HoldId is provided, caller must own the hold (or be staff).
    /// Simulates success (ends with 0001), card decline (0002), or timeout (0003).
    /// </summary>
    [HttpPost("confirm-sandbox-charge")]
    public async Task<ActionResult<PaymentChargeResponseDto>> ProcessSandboxCharge(
        [FromBody] PaymentChargeRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CardNumber))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = "Card number is required."
            });
        }

        if (request.Amount <= 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Amount",
                Detail = "Charge amount must be greater than zero."
            });
        }

        var userId = GetCurrentUserId();
        var isStaff = IsCallerStaff();

        // If request references a specific HoldId, enforce ownership
        if (request.HoldId.HasValue && _context != null)
        {
            var hold = await _context.SeatHolds
                .Include(h => h.Passenger)
                .FirstOrDefaultAsync(h => h.Id == request.HoldId.Value, cancellationToken);

            if (hold == null)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Hold Not Found",
                    Detail = $"Seat hold '{request.HoldId.Value}' was not found."
                });
            }

            if (!isStaff && userId.HasValue && hold.Passenger?.UserId != userId.Value)
            {
                _logger.LogWarning(
                    "Payment ownership violation: User {UserId} attempted to process payment for hold {HoldId} belonging to another passenger",
                    userId.Value, request.HoldId.Value);
                return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Access Denied",
                    Detail = "You do not have permission to process payments for another passenger's seat hold."
                });
            }
        }

        _logger.LogInformation("Processing sandbox payment of Rs. {Amount} for card ending in {Last4}",
            request.Amount,
            request.CardNumber.Length >= 4 ? request.CardNumber[^4..] : "****");

        var response = await _bookingService.ProcessSandboxPaymentAsync(request, cancellationToken);

        if (!response.IsSuccess)
        {
            if (response.GatewayStatus == "Timeout")
            {
                return StatusCode(StatusCodes.Status504GatewayTimeout, response);
            }

            return StatusCode(StatusCodes.Status402PaymentRequired, response);
        }

        return Ok(response);
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
