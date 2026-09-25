 using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WayPoint.Application.Common.Interfaces.Fleet;
using WayPoint.Application.Features.FleetManagement.DTOs;

namespace WayPoint.API.Controllers;

[ApiController]
[Route("api/v1/reviews")]
public class ReviewController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    private Guid GetPassengerId()
    {
        var passengerIdStr = User.FindFirst("PassengerId")?.Value;
        if (string.IsNullOrEmpty(passengerIdStr) || !Guid.TryParse(passengerIdStr, out var passengerId))
        {
            throw new UnauthorizedAccessException("User is not a registered passenger.");
        }
        return passengerId;
    }

    [HttpPost("buses")]
    [Authorize(Policy = "RequirePassenger")]
    public async Task<IActionResult> SubmitBusReview([FromBody] CreateBusReviewDto request)
    {
        try
        {
            request.PassengerId = GetPassengerId();
            var review = await _reviewService.SubmitBusReviewAsync(request);
            return CreatedAtAction(nameof(GetBusReviews), new { busId = request.BusId }, review);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation Error",
                Status = StatusCodes.Status400BadRequest,
                Detail = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Business Rule Conflict",
                Status = StatusCodes.Status409Conflict,
                Detail = ex.Message
            });
        }
    }

    [HttpPost("drivers")]
    [Authorize(Policy = "RequirePassenger")]
    public async Task<IActionResult> SubmitDriverReview([FromBody] CreateDriverReviewDto request)
    {
        try
        {
            request.PassengerId = GetPassengerId();
            var review = await _reviewService.SubmitDriverReviewAsync(request);
            return CreatedAtAction(nameof(GetDriverReviews), new { driverId = request.DriverId }, review);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation Error",
                Status = StatusCodes.Status400BadRequest,
                Detail = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Business Rule Conflict",
                Status = StatusCodes.Status409Conflict,
                Detail = ex.Message
            });
        }
    }

    [HttpPut("buses/{reviewId:guid}")]
    [Authorize(Policy = "RequirePassenger")]
    public async Task<IActionResult> UpdateBusReview(Guid reviewId, [FromBody] UpdateReviewDto request)
    {
        try
        {
            var review = await _reviewService.UpdateBusReviewAsync(reviewId, request);
            return Ok(review);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Window Expired",
                Status = StatusCodes.Status400BadRequest,
                Detail = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation Error",
                Status = StatusCodes.Status400BadRequest,
                Detail = ex.Message
            });
        }
    }

    [HttpPut("drivers/{reviewId:guid}")]
    [Authorize(Policy = "RequirePassenger")]
    public async Task<IActionResult> UpdateDriverReview(Guid reviewId, [FromBody] UpdateReviewDto request)
    {
        try
        {
            var review = await _reviewService.UpdateDriverReviewAsync(reviewId, request);
            return Ok(review);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Window Expired",
                Status = StatusCodes.Status400BadRequest,
                Detail = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation Error",
                Status = StatusCodes.Status400BadRequest,
                Detail = ex.Message
            });
        }
    }

    [HttpDelete("buses/{reviewId:guid}")]
    [Authorize(Policy = "RequirePassenger")]
    public async Task<IActionResult> DeleteBusReview(Guid reviewId)
    {
        try
        {
            var passengerId = GetPassengerId();
            await _reviewService.DeleteBusReviewAsync(reviewId, passengerId);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Forbidden",
                Status = StatusCodes.Status403Forbidden,
                Detail = ex.Message
            });
        }
    }

    [HttpDelete("drivers/{reviewId:guid}")]
    [Authorize(Policy = "RequirePassenger")]
    public async Task<IActionResult> DeleteDriverReview(Guid reviewId)
    {
        try
        {
            var passengerId = GetPassengerId();
            await _reviewService.DeleteDriverReviewAsync(reviewId, passengerId);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Forbidden",
                Status = StatusCodes.Status403Forbidden,
                Detail = ex.Message
            });
        }
    }

    [HttpGet("buses/{busId:guid}")]
    [Authorize(Policy = "RequireOperator")]
    public async Task<IActionResult> GetBusReviews(Guid busId, [FromQuery] ReviewFilterParams filter)
    {
        var result = await _reviewService.GetBusReviewsAsync(busId, filter);
        return Ok(result);
    }

    [HttpGet("drivers/{driverId:guid}")]
    [Authorize(Policy = "RequireOperator")]
    public async Task<IActionResult> GetDriverReviews(Guid driverId, [FromQuery] ReviewFilterParams filter)
    {
        var result = await _reviewService.GetDriverReviewsAsync(driverId, filter);
        return Ok(result);
    }

    [HttpGet("buses/{busId:guid}/summary")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBusRatingSummary(Guid busId)
    {
        try
        {
            var summary = await _reviewService.GetBusRatingSummaryAsync(busId);
            return Ok(summary);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("drivers/{driverId:guid}/summary")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDriverRatingSummary(Guid driverId)
    {
        try
        {
            var summary = await _reviewService.GetDriverRatingSummaryAsync(driverId);
            return Ok(summary);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
