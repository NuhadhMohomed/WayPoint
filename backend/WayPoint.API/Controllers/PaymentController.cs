using Microsoft.AspNetCore.Mvc;
using WayPoint.Application.Common.Interfaces;
using WayPoint.Application.DTOs.Booking;

namespace WayPoint.API.Controllers;

[ApiController]
[Route("api/v1/payments")]
public class PaymentController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(IBookingService bookingService, ILogger<PaymentController> logger)
    {
        _bookingService = bookingService;
        _logger = logger;
    }

    /// <summary>
    /// Processes a simulated payment card charge against the sandbox gateway (US-PASS-004).
    /// Simulates success (ends with 0001), card decline (0002), or timeout (0003).
    /// </summary>
    [HttpPost("sandbox-charge")]
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
}
