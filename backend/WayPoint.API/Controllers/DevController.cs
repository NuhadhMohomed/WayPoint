using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WayPoint.Application.Common.Interfaces;
using WayPoint.Infrastructure.Data;

namespace WayPoint.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class DevController : ControllerBase
{
    private readonly WayPointDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DevController> _logger;

    public DevController(
        WayPointDbContext context,
        IPasswordHasher passwordHasher,
        ILogger<DevController> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    [HttpPost("seed")]
    public async Task<IActionResult> SeedDatabase()
    {
        try
        {
            _logger.LogInformation("Applying pending database migrations...");
            await _context.Database.MigrateAsync();

            _logger.LogInformation("Seeding database with realistic transit data...");
            await DbSeeder.SeedAsync(_context, _passwordHasher);

            var summary = new
            {
                message = "Database seeded successfully.",
                roles = await _context.Roles.CountAsync(),
                users = await _context.Users.CountAsync(),
                routes = await _context.Routes.CountAsync(),
                stops = await _context.RouteStops.CountAsync(),
                buses = await _context.Buses.CountAsync(),
                seats = await _context.Seats.CountAsync(),
                services = await _context.Services.CountAsync()
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed database.");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Seeding Failed",
                Detail = ex.Message
            });
        }
    }

    [HttpPost("mock-trip-for-review")]
    public async Task<IActionResult> CreateMockTripForReview([FromBody] WayPoint.Application.Features.FleetManagement.DTOs.CreateBusReviewDto request)
    {
        // This is a dev-only mock to satisfy ReviewService business rules
        var route = await _context.Routes.FirstOrDefaultAsync();
        if (route == null) return NotFound("No routes available.");

        var service = new WayPoint.Domain.Entities.Journey.Service
        {
            ServiceCode = "MOCK-SRV-" + Guid.NewGuid().ToString()[..4],
            RouteId = route.Id,
            BusId = request.BusId,
            DriverId = request.BookingId, // Hack to pass driverId through BookingId field in this mock payload
            DepartureTime = DateTime.UtcNow.AddHours(-10),
            ArrivalTime = DateTime.UtcNow.AddHours(-5),
            BaseFare = 1000,
            Status = WayPoint.Domain.Enums.ServiceStatus.Completed
        };
        await _context.Services.AddAsync(service);
        await _context.SaveChangesAsync();

        var passengerProfile = await _context.Set<WayPoint.Domain.Entities.Identity.PassengerProfile>().FirstOrDefaultAsync(p => p.UserId == request.PassengerId);
        if (passengerProfile == null) return NotFound("Passenger profile not found for user.");

        var booking = new WayPoint.Domain.Entities.Booking.Booking
        {
            BookingReference = "MOCK-" + Guid.NewGuid().ToString()[..6],
            PassengerId = passengerProfile.Id,
            ServiceId = service.Id,
            Status = WayPoint.Domain.Enums.BookingStatus.Confirmed,
            TotalFareAmount = 1000,
            SeatNumbers = "1A"
        };
        await _context.Bookings.AddAsync(booking);
        await _context.SaveChangesAsync();

        return Ok(new { bookingId = booking.Id });
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetDatabaseStatus()
    {
        var canConnect = await _context.Database.CanConnectAsync();
        if (!canConnect)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                database = "Disconnected",
                canConnect = false
            });
        }

        return Ok(new
        {
            database = "Connected",
            canConnect = true,
            provider = _context.Database.ProviderName,
            counts = new
            {
                users = await _context.Users.CountAsync(),
                routes = await _context.Routes.CountAsync(),
                services = await _context.Services.CountAsync(),
                buses = await _context.Buses.CountAsync()
            }
        });
    }
}
