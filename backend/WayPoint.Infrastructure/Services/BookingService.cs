using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WayPoint.Application.Common.Interfaces;
using WayPoint.Application.DTOs.Booking;
using WayPoint.Application.DTOs.Common;
using WayPoint.Domain.Entities.Booking;
using WayPoint.Domain.Entities.Identity;
using WayPoint.Domain.Enums;
using WayPoint.Infrastructure.Data;

namespace WayPoint.Infrastructure.Services;

public class BookingService : IBookingService
{
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _serviceLocks = new();
    private readonly WayPointDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ICurrentUserService? _currentUserService;

    public BookingService(
        WayPointDbContext context,
        IConfiguration configuration,
        ICurrentUserService? currentUserService = null)
    {
        _context = context;
        _configuration = configuration;
        _currentUserService = currentUserService;
    }

    public async Task<List<ServiceSummaryDto>> GetAvailableServicesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Services
            .Include(s => s.Route)
            .Include(s => s.Bus)
            .OrderBy(s => s.DepartureTime)
            .Select(s => new ServiceSummaryDto
            {
                Id = s.Id,
                ServiceCode = s.ServiceCode,
                RouteName = s.Route.Name,
                OriginCity = s.Route.OriginCity,
                DestinationCity = s.Route.DestinationCity,
                DepartureTime = s.DepartureTime,
                ArrivalTime = s.ArrivalTime,
                BaseFare = s.BaseFare,
                BusRegistration = s.Bus.RegistrationNumber,
                BusClass = s.Bus.BusClass.ToString(),
                TotalSeats = s.Bus.TotalSeatCapacity
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<SeatHoldResponseDto> CreateSeatHoldAsync(SeatHoldRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.SeatNumbers == null || request.SeatNumbers.Count == 0)
        {
            throw new ArgumentException("At least one seat number must be specified.", nameof(request));
        }

        var service = request.ServiceId != Guid.Empty
            ? await _context.Services
                .Include(s => s.Route)
                .Include(s => s.Bus)
                    .ThenInclude(b => b.SeatLayout)
                .FirstOrDefaultAsync(s => s.Id == request.ServiceId, cancellationToken)
            : await _context.Services
                .Include(s => s.Route)
                .Include(s => s.Bus)
                    .ThenInclude(b => b.SeatLayout)
                .FirstOrDefaultAsync(cancellationToken);

        if (service == null)
        {
            throw new KeyNotFoundException($"Scheduled service with ID '{request.ServiceId}' was not found.");
        }

        // 1. Resolve passenger profile using trusted caller identity (BOLA / Ownership enforcement)
        PassengerProfile? passengerProfile = null;

        if (_currentUserService != null && _currentUserService.IsAuthenticated)
        {
            var currentUserId = _currentUserService.UserId;
            if (currentUserId.HasValue)
            {
                passengerProfile = await _context.PassengerProfiles
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.UserId == currentUserId.Value, cancellationToken);
            }

            // Spoofing check: non-staff cannot specify another passenger's ID
            if (request.PassengerId.HasValue && passengerProfile != null &&
                request.PassengerId.Value != passengerProfile.Id &&
                request.PassengerId.Value != passengerProfile.UserId &&
                !_currentUserService.IsStaff)
            {
                throw new UnauthorizedAccessException("Cannot create seat hold for another passenger.");
            }
        }
        else if (request.PassengerId.HasValue)
        {
            passengerProfile = await _context.PassengerProfiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == request.PassengerId.Value || p.UserId == request.PassengerId.Value, cancellationToken);
        }

        if (passengerProfile == null)
        {
            throw new InvalidOperationException("No valid passenger profile found to associate with seat hold.");
        }

        // 2. Fetch seats from bus layout - ordered deterministically by Id to avoid deadlocks (Dijkstra resource hierarchy)
        var layoutSeats = await _context.Seats
            .Where(s => s.SeatLayoutId == service.Bus.SeatLayoutId && request.SeatNumbers.Contains(s.SeatNumber))
            .OrderBy(s => s.Id)
            .ToListAsync(cancellationToken);

        if (layoutSeats.Count < request.SeatNumbers.Count)
        {
            var foundNumbers = layoutSeats.Select(s => s.SeatNumber).ToHashSet();
            var missing = request.SeatNumbers.Where(num => !foundNumbers.Contains(num));
            throw new InvalidOperationException($"Invalid seat numbers for this bus: {string.Join(", ", missing)}");
        }

        // 3. Dual-Layer Concurrency Control: Per-service in-process lock + DB transaction & unique constraints
        var semaphore = _serviceLocks.GetOrAdd(service.Id, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var now = DateTime.UtcNow;

                    // 3a. Clean up expired hold allocations for this service so seats become available again
                    var expiredAllocations = await _context.SeatAllocations
                        .Where(sa => sa.ServiceId == service.Id &&
                                     sa.AllocationType == SeatAllocationType.Hold &&
                                     sa.ExpiresAt <= now)
                        .ToListAsync(cancellationToken);

                    if (expiredAllocations.Count > 0)
                    {
                        _context.SeatAllocations.RemoveRange(expiredAllocations);
                    }

                    var expiredHolds = await _context.SeatHolds
                        .Where(sh => sh.ServiceId == service.Id &&
                                     sh.Status == SeatHoldStatus.Held &&
                                     sh.HeldUntil <= now)
                        .ToListAsync(cancellationToken);

                    foreach (var expiredHold in expiredHolds)
                    {
                        expiredHold.Status = SeatHoldStatus.Expired;
                    }

                    if (expiredAllocations.Count > 0 || expiredHolds.Count > 0)
                    {
                        await _context.SaveChangesAsync(cancellationToken);
                    }

                    var seatIds = layoutSeats.Select(s => s.Id).ToList();

                    // 3b. Verify no active seat allocations exist in the authoritative SeatAllocations table
                    var activeAllocations = await _context.SeatAllocations
                        .Include(sa => sa.Seat)
                        .Where(sa => sa.ServiceId == service.Id &&
                                     seatIds.Contains(sa.SeatId) &&
                                     sa.ExpiresAt > now)
                        .ToListAsync(cancellationToken);

                    if (activeAllocations.Count > 0)
                    {
                        var conflictedSeatNumbers = string.Join(", ", activeAllocations.Select(a => a.Seat?.SeatNumber ?? a.SeatId.ToString()));
                        throw new InvalidOperationException($"Seat(s) {conflictedSeatNumbers} are currently held by another passenger. Please select alternative seats.");
                    }

                    // 3c. Verify no legacy active holds exist in SeatHolds table
                    var activeHolds = await _context.SeatHolds
                        .Where(sh => sh.ServiceId == service.Id &&
                                     seatIds.Contains(sh.SeatId) &&
                                     sh.Status == SeatHoldStatus.Held &&
                                     sh.HeldUntil > now)
                        .Include(sh => sh.Seat)
                        .ToListAsync(cancellationToken);

                    if (activeHolds.Count != 0)
                    {
                        var heldSeatNumbers = string.Join(", ", activeHolds.Select(h => h.Seat.SeatNumber));
                        throw new InvalidOperationException($"Seat(s) {heldSeatNumbers} are currently held by another passenger. Please select alternative seats.");
                    }

                    // 3d. Verify no confirmed bookings occupy any requested seat
                    var confirmedBookings = await _context.Bookings
                        .Where(b => b.ServiceId == service.Id && b.Status == BookingStatus.Confirmed)
                        .ToListAsync(cancellationToken);

                    foreach (var seatNum in request.SeatNumbers)
                    {
                        var isBooked = confirmedBookings.Any(b => b.SeatNumbers.Split(',', StringSplitOptions.TrimEntries).Contains(seatNum));
                        if (isBooked)
                        {
                            throw new InvalidOperationException($"Seat {seatNum} is already booked and unavailable.");
                        }
                    }

                    // 4. Create 10-Minute Seat Holds and DB-Constrained Seat Allocations
                    var heldAt = DateTime.UtcNow;
                    var heldUntil = heldAt.AddMinutes(10);
                    var createdHolds = new List<SeatHold>();
                    var createdAllocations = new List<SeatAllocation>();

                    foreach (var seat in layoutSeats)
                    {
                        var hold = new SeatHold
                        {
                            Id = Guid.NewGuid(),
                            ServiceId = service.Id,
                            SeatId = seat.Id,
                            PassengerId = passengerProfile.Id,
                            HeldAt = heldAt,
                            HeldUntil = heldUntil,
                            Status = SeatHoldStatus.Held
                        };
                        createdHolds.Add(hold);

                        var allocation = new SeatAllocation
                        {
                            Id = Guid.NewGuid(),
                            ServiceId = service.Id,
                            SeatId = seat.Id,
                            HoldId = hold.Id,
                            PassengerId = passengerProfile.Id,
                            AllocatedAt = heldAt,
                            ExpiresAt = heldUntil,
                            AllocationType = SeatAllocationType.Hold
                        };
                        createdAllocations.Add(allocation);
                    }

                    await _context.SeatHolds.AddRangeAsync(createdHolds, cancellationToken);
                    await _context.SeatAllocations.AddRangeAsync(createdAllocations, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);

                    await transaction.CommitAsync(cancellationToken);

                    var primaryHold = createdHolds.First();
                    var totalAmount = service.BaseFare * request.SeatNumbers.Count;

                    return new SeatHoldResponseDto
                    {
                        HoldId = primaryHold.Id,
                        ServiceId = service.Id,
                        ServiceCode = service.ServiceCode,
                        RouteTitle = service.Route != null ? $"{service.Route.OriginCity} - {service.Route.DestinationCity} ({service.Route.Name})" : service.ServiceCode,
                        SeatNumbers = request.SeatNumbers,
                        HeldAt = heldAt,
                        HeldUntil = heldUntil,
                        SecondsRemaining = (int)(heldUntil - DateTime.UtcNow).TotalSeconds,
                        BaseFarePerSeat = service.BaseFare,
                        TotalAmount = totalAmount,
                        Status = "Held"
                    };
                }
                catch (DbUpdateException ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw new InvalidOperationException("One or more selected seats were concurrently allocated to another passenger. Please select alternative seats.", ex);
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task<bool> ReleaseSeatHoldAsync(Guid holdId, CancellationToken cancellationToken = default)
    {
        var hold = await _context.SeatHolds
            .Include(h => h.Passenger)
            .FirstOrDefaultAsync(h => h.Id == holdId, cancellationToken);
        if (hold == null) return false;

        // BOLA / Ownership enforcement: caller must own the hold unless staff
        if (_currentUserService != null && _currentUserService.IsAuthenticated && !_currentUserService.IsStaff)
        {
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue || hold.Passenger?.UserId != currentUserId.Value)
            {
                throw new UnauthorizedAccessException("You do not have permission to release this seat hold.");
            }
        }

        if (hold.Status == SeatHoldStatus.Held)
        {
            hold.Status = SeatHoldStatus.Expired;

            // Remove corresponding seat allocation to make seat immediately available
            var allocations = await _context.SeatAllocations
                .Where(sa => sa.HoldId == holdId || (sa.ServiceId == hold.ServiceId && sa.SeatId == hold.SeatId && sa.HoldId == hold.Id))
                .ToListAsync(cancellationToken);

            if (allocations.Count > 0)
            {
                _context.SeatAllocations.RemoveRange(allocations);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        return false;
    }

    public async Task<SeatHoldResponseDto?> GetSeatHoldStatusAsync(Guid holdId, CancellationToken cancellationToken = default)
    {
        var hold = await _context.SeatHolds
            .Include(h => h.Service)
                .ThenInclude(s => s.Route)
            .Include(h => h.Seat)
            .Include(h => h.Passenger)
            .FirstOrDefaultAsync(h => h.Id == holdId, cancellationToken);

        if (hold == null) return null;

        // BOLA / Ownership enforcement: caller must own the hold unless staff
        if (_currentUserService != null && _currentUserService.IsAuthenticated && !_currentUserService.IsStaff)
        {
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue || hold.Passenger?.UserId != currentUserId.Value)
            {
                throw new UnauthorizedAccessException("You do not have permission to view this seat hold.");
            }
        }

        if (hold.Status == SeatHoldStatus.Held && DateTime.UtcNow > hold.HeldUntil)
        {
            hold.Status = SeatHoldStatus.Expired;

            // Remove expired seat allocation
            var allocations = await _context.SeatAllocations
                .Where(sa => sa.HoldId == hold.Id)
                .ToListAsync(cancellationToken);

            if (allocations.Count > 0)
            {
                _context.SeatAllocations.RemoveRange(allocations);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        var secondsRemaining = Math.Max(0, (int)(hold.HeldUntil - DateTime.UtcNow).TotalSeconds);

        return new SeatHoldResponseDto
        {
            HoldId = hold.Id,
            ServiceId = hold.ServiceId,
            ServiceCode = hold.Service.ServiceCode,
            RouteTitle = hold.Service.Route != null ? $"{hold.Service.Route.OriginCity} - {hold.Service.Route.DestinationCity}" : hold.Service.ServiceCode,
            SeatNumbers = new List<string> { hold.Seat.SeatNumber },
            HeldAt = hold.HeldAt,
            HeldUntil = hold.HeldUntil,
            SecondsRemaining = secondsRemaining,
            BaseFarePerSeat = hold.Service.BaseFare,
            TotalAmount = hold.Service.BaseFare,
            Status = hold.Status.ToString()
        };
    }

    public async Task<PaymentChargeResponseDto> ProcessSandboxPaymentAsync(PaymentChargeRequestDto request, CancellationToken cancellationToken = default)
    {
        // Simulate real card gateway processing latency
        await Task.Delay(300, cancellationToken);

        var cleanCard = request.CardNumber.Replace(" ", "").Trim();

        // Preset 2: Simulated card decline / insufficient funds (Card ending in 0002)
        if (cleanCard.EndsWith("0002"))
        {
            var failedTxnId = $"TXN-FAIL-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";
            if (_context != null)
            {
                Guid? passengerId = null;
                if (request.HoldId.HasValue)
                {
                    var hold = await _context.SeatHolds
                        .Include(h => h.Passenger)
                        .FirstOrDefaultAsync(h => h.Id == request.HoldId.Value, cancellationToken);
                    passengerId = hold?.PassengerId;
                }
                else if (_currentUserService?.IsAuthenticated == true && _currentUserService.UserId.HasValue)
                {
                    var profile = await _context.PassengerProfiles
                        .FirstOrDefaultAsync(p => p.UserId == _currentUserService.UserId.Value, cancellationToken);
                    passengerId = profile?.Id;
                }

                var attempt = new PaymentAttempt
                {
                    Id = Guid.NewGuid(),
                    GatewayTransactionId = failedTxnId,
                    Amount = request.Amount,
                    Currency = "LKR",
                    Status = PaymentStatus.Failed,
                    HoldId = request.HoldId,
                    PassengerId = passengerId,
                    ProcessedAt = DateTime.UtcNow
                };
                await _context.PaymentAttempts.AddAsync(attempt, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return new PaymentChargeResponseDto
            {
                IsSuccess = false,
                TransactionId = failedTxnId,
                GatewayStatus = "Declined",
                Message = "402 Payment Required: Insufficient Funds. Your card issuer declined this transaction.",
                ProcessedAt = DateTime.UtcNow
            };
        }

        // Preset 3: Simulated gateway timeout (Card ending in 0003)
        if (cleanCard.EndsWith("0003"))
        {
            var pendingTxnId = $"TXN-PEND-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";
            if (_context != null)
            {
                Guid? passengerId = null;
                if (request.HoldId.HasValue)
                {
                    var hold = await _context.SeatHolds
                        .Include(h => h.Passenger)
                        .FirstOrDefaultAsync(h => h.Id == request.HoldId.Value, cancellationToken);
                    passengerId = hold?.PassengerId;
                }
                else if (_currentUserService?.IsAuthenticated == true && _currentUserService.UserId.HasValue)
                {
                    var profile = await _context.PassengerProfiles
                        .FirstOrDefaultAsync(p => p.UserId == _currentUserService.UserId.Value, cancellationToken);
                    passengerId = profile?.Id;
                }

                var attempt = new PaymentAttempt
                {
                    Id = Guid.NewGuid(),
                    GatewayTransactionId = pendingTxnId,
                    Amount = request.Amount,
                    Currency = "LKR",
                    Status = PaymentStatus.Pending,
                    HoldId = request.HoldId,
                    PassengerId = passengerId,
                    ProcessedAt = DateTime.UtcNow
                };
                await _context.PaymentAttempts.AddAsync(attempt, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return new PaymentChargeResponseDto
            {
                IsSuccess = false,
                TransactionId = pendingTxnId,
                GatewayStatus = "Timeout",
                Message = "504 Gateway Timeout: The payment network did not respond within the allocated timeframe.",
                ProcessedAt = DateTime.UtcNow
            };
        }

        // Preset 1: Instant Success (Card ending in 0001 or standard test cards)
        var txnId = $"TXN-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";
        if (_context != null)
        {
            Guid? passengerId = null;
            if (request.HoldId.HasValue)
            {
                var hold = await _context.SeatHolds
                    .Include(h => h.Passenger)
                    .FirstOrDefaultAsync(h => h.Id == request.HoldId.Value, cancellationToken);
                passengerId = hold?.PassengerId;
            }
            else if (_currentUserService?.IsAuthenticated == true && _currentUserService.UserId.HasValue)
            {
                var profile = await _context.PassengerProfiles
                    .FirstOrDefaultAsync(p => p.UserId == _currentUserService.UserId.Value, cancellationToken);
                passengerId = profile?.Id;
            }

            var attempt = new PaymentAttempt
            {
                Id = Guid.NewGuid(),
                GatewayTransactionId = txnId,
                Amount = request.Amount,
                Currency = "LKR",
                Status = PaymentStatus.Success,
                HoldId = request.HoldId,
                PassengerId = passengerId,
                ProcessedAt = DateTime.UtcNow
            };
            await _context.PaymentAttempts.AddAsync(attempt, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new PaymentChargeResponseDto
        {
            IsSuccess = true,
            TransactionId = txnId,
            GatewayStatus = "Success",
            Message = "Payment authorized and settled successfully via WayPoint Sandbox Gateway.",
            ProcessedAt = DateTime.UtcNow
        };
    }

    public async Task<BookingConfirmationDto> ExecuteCheckoutAsync(BookingCheckoutRequestDto request, CancellationToken cancellationToken = default)
    {
        // 1. Initial parameter sanity checks
        if (string.IsNullOrWhiteSpace(request.PaymentTransactionId))
        {
            throw new InvalidOperationException("Payment transaction ID is required.");
        }

        if (request.PaymentTransactionId.StartsWith("FAIL", StringComparison.OrdinalIgnoreCase) ||
            request.PaymentTransactionId.StartsWith("DECLINE", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Payment failed or card was declined. Transaction rolled back.");
        }

        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // 2. Fetch the primary hold
                var hold = await _context.SeatHolds
                    .Include(h => h.Service)
                        .ThenInclude(s => s.Route)
                    .Include(h => h.Seat)
                    .Include(h => h.Passenger)
                        .ThenInclude(p => p.User)
                    .FirstOrDefaultAsync(h => h.Id == request.HoldId, cancellationToken);

                if (hold == null)
                {
                    throw new KeyNotFoundException("Active seat hold was not found.");
                }

                // 3. BOLA / Ownership enforcement: caller must own the seat hold being checked out (Security Fix 01)
                if (_currentUserService != null && _currentUserService.IsAuthenticated && !_currentUserService.IsStaff)
                {
                    var currentUserId = _currentUserService.UserId;
                    if (!currentUserId.HasValue || hold.Passenger?.UserId != currentUserId.Value)
                    {
                        throw new UnauthorizedAccessException("You do not have permission to checkout this seat hold.");
                    }
                }

                // 4. Server-side payment transaction verification (Security Fix 02)
                var payment = await _context.PaymentAttempts
                    .FirstOrDefaultAsync(p => p.GatewayTransactionId == request.PaymentTransactionId, cancellationToken);

                if (payment == null)
                {
                    throw new InvalidOperationException("Payment transaction does not exist or could not be verified in server records.");
                }

                // 4a. Only transactions explicitly marked successful may confirm a booking
                if (payment.Status != PaymentStatus.Success)
                {
                    throw new InvalidOperationException($"Payment transaction has status '{payment.Status}' and cannot confirm a booking. Only successful transactions are accepted.");
                }

                // 4b. Verify payment currency
                if (!string.Equals(payment.Currency, "LKR", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Payment currency '{payment.Currency}' is invalid. WayPoint requires LKR.");
                }

                // 4c. Verify payment belongs to the correct passenger
                if (payment.PassengerId.HasValue && payment.PassengerId.Value != hold.PassengerId)
                {
                    throw new UnauthorizedAccessException("Payment transaction does not belong to the authenticated passenger.");
                }

                // 4d. Idempotency & Replay Protection: Check if payment was already used
                if (payment.BookingId.HasValue)
                {
                    // Check if this is a duplicate checkout request for the same booking
                    if (payment.HoldId == hold.Id || hold.Status == SeatHoldStatus.ConvertedToBooking)
                    {
                        var existingBooking = await _context.Bookings
                            .Include(b => b.Service)
                                .ThenInclude(s => s.Route)
                            .Include(b => b.Ticket)
                            .Include(b => b.Passenger)
                                .ThenInclude(p => p.User)
                            .FirstOrDefaultAsync(b => b.Id == payment.BookingId.Value, cancellationToken);

                        if (existingBooking != null)
                        {
                            var existingRoute = existingBooking.Service?.Route;
                            var existingSeats = existingBooking.SeatNumbers.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

                            return new BookingConfirmationDto
                            {
                                BookingId = existingBooking.Id,
                                BookingReference = existingBooking.BookingReference,
                                ServiceId = existingBooking.ServiceId,
                                ServiceCode = existingBooking.Service?.ServiceCode ?? "",
                                RouteTitle = existingRoute != null ? $"{existingRoute.OriginCity} - {existingRoute.DestinationCity} ({existingRoute.Name})" : (existingBooking.Service?.ServiceCode ?? ""),
                                OriginCity = existingRoute?.OriginCity ?? "Makumbura MMC (Colombo)",
                                DestinationCity = existingRoute?.DestinationCity ?? "Ella City Station",
                                DepartureTime = existingBooking.Service?.DepartureTime ?? DateTime.UtcNow,
                                ArrivalTime = existingBooking.Service?.ArrivalTime ?? DateTime.UtcNow,
                                SeatNumbers = existingSeats,
                                TotalFareAmount = existingBooking.TotalFareAmount,
                                Status = existingBooking.Status.ToString(),
                                ConfirmedAt = existingBooking.CreatedAt,
                                TicketId = existingBooking.Ticket?.Id ?? Guid.Empty,
                                QrCodePayload = existingBooking.Ticket?.QrCodePayload ?? ""
                            };
                        }
                    }

                    // Attempted reuse of an already confirmed transaction for an unrelated booking
                    throw new InvalidOperationException("Payment transaction has already been used to confirm another booking.");
                }

                // 4e. Verify payment is linked to this seat hold
                if (payment.HoldId.HasValue && payment.HoldId.Value != hold.Id)
                {
                    throw new InvalidOperationException("Payment transaction is linked to a different seat hold.");
                }

                // 5. Server-side validation of 10-minute hold window
                if (hold.Status != SeatHoldStatus.Held || hold.HeldUntil <= DateTime.UtcNow)
                {
                    hold.Status = SeatHoldStatus.Expired;
                    var expiredAllocations = await _context.SeatAllocations
                        .Where(sa => sa.HoldId == hold.Id)
                        .ToListAsync(cancellationToken);
                    if (expiredAllocations.Count > 0)
                    {
                        _context.SeatAllocations.RemoveRange(expiredAllocations);
                    }
                    await _context.SaveChangesAsync(cancellationToken);
                    await transaction.RollbackAsync(cancellationToken);
                    throw new InvalidOperationException("Seat hold has expired. Please re-select your seats.");
                }

                // 6. Find all sibling holds for this passenger, service, and hold timestamp
                var batchHolds = await _context.SeatHolds
                    .Where(sh => sh.ServiceId == hold.ServiceId &&
                                 sh.PassengerId == hold.PassengerId &&
                                 sh.Status == SeatHoldStatus.Held &&
                                 sh.HeldUntil > DateTime.UtcNow)
                    .Include(sh => sh.Seat)
                    .ToListAsync(cancellationToken);

                var seatNumbersList = batchHolds.Select(h => h.Seat.SeatNumber).Distinct().OrderBy(s => s).ToList();
                var totalFare = hold.Service.BaseFare * seatNumbersList.Count;

                // 6b. Verify seat allocations: ensure no conflicting valid allocations exist (Security Fix 03)
                var batchSeatIds = batchHolds.Select(h => h.SeatId).ToList();
                var existingAllocations = await _context.SeatAllocations
                    .Where(sa => sa.ServiceId == hold.ServiceId && batchSeatIds.Contains(sa.SeatId))
                    .ToListAsync(cancellationToken);

                // Any existing allocation must belong to this passenger and not be expired
                foreach (var alloc in existingAllocations)
                {
                    if (alloc.PassengerId != hold.PassengerId || alloc.ExpiresAt <= DateTime.UtcNow)
                    {
                        throw new InvalidOperationException("A conflicting seat allocation exists or hold has expired. Checkout cannot proceed.");
                    }
                }

                // 7. Verify payment amount matches required total fare
                if (payment.Amount != totalFare)
                {
                    throw new InvalidOperationException($"Payment amount mismatch. Transaction amount: {payment.Amount:F2}, required fare: {totalFare:F2}.");
                }

                // 8. Generate unique 8-character Booking Reference: WP-XXXXXX
                var randomSuffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
                var bookingRef = $"WP-{randomSuffix}";

                var passengerName = !string.IsNullOrWhiteSpace(request.PassengerName)
                    ? request.PassengerName
                    : (hold.Passenger?.User?.FullName ?? "Nimal Silva");

                // 9. Create Booking Entity
                var booking = new Booking
                {
                    Id = Guid.NewGuid(),
                    BookingReference = bookingRef,
                    PassengerId = hold.PassengerId,
                    ServiceId = hold.ServiceId,
                    SeatNumbers = string.Join(",", seatNumbersList),
                    TotalFareAmount = totalFare,
                    Status = BookingStatus.Confirmed
                };
                await _context.Bookings.AddAsync(booking, cancellationToken);

                // 10. Update and Link Payment Attempt
                payment.BookingId = booking.Id;
                payment.HoldId = hold.Id;
                payment.PassengerId = hold.PassengerId;

                // 11. Generate HMAC-SHA256 Cryptographic QR Ticket Payload
                var qrPayload = await GenerateTicketPayloadAsync(
                    bookingRef,
                    hold.Service.ServiceCode,
                    string.Join(",", seatNumbersList),
                    passengerName
                );

                // 12. Issue Digital Ticket
                var ticket = new Ticket
                {
                    BookingId = booking.Id,
                    QrCodePayload = qrPayload,
                    Status = TicketStatus.Issued,
                    IsBoarded = false
                };
                await _context.Tickets.AddAsync(ticket, cancellationToken);

                // 13. Transition SeatHolds to ConvertedToBooking
                foreach (var h in batchHolds)
                {
                    h.Status = SeatHoldStatus.ConvertedToBooking;
                }

                // 14. Convert SeatAllocations to permanent Booking allocations
                var allocatedSeatIds = existingAllocations.Select(a => a.SeatId).ToHashSet();
                foreach (var alloc in existingAllocations)
                {
                    alloc.AllocationType = SeatAllocationType.Booking;
                    alloc.BookingId = booking.Id;
                    alloc.ExpiresAt = DateTime.MaxValue;
                }

                // For any batch seats that had no prior allocation record (e.g. legacy test seeds)
                foreach (var h in batchHolds)
                {
                    if (!allocatedSeatIds.Contains(h.SeatId))
                    {
                        var newAlloc = new SeatAllocation
                        {
                            Id = Guid.NewGuid(),
                            ServiceId = hold.ServiceId,
                            SeatId = h.SeatId,
                            BookingId = booking.Id,
                            PassengerId = hold.PassengerId,
                            AllocatedAt = DateTime.UtcNow,
                            ExpiresAt = DateTime.MaxValue,
                            AllocationType = SeatAllocationType.Booking
                        };
                        await _context.SeatAllocations.AddAsync(newAlloc, cancellationToken);
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var route = hold.Service.Route;
                return new BookingConfirmationDto
                {
                    BookingId = booking.Id,
                    BookingReference = bookingRef,
                    ServiceId = hold.ServiceId,
                    ServiceCode = hold.Service.ServiceCode,
                    RouteTitle = route != null ? $"{route.OriginCity} - {route.DestinationCity} ({route.Name})" : hold.Service.ServiceCode,
                    OriginCity = route?.OriginCity ?? "Makumbura MMC (Colombo)",
                    DestinationCity = route?.DestinationCity ?? "Ella City Station",
                    DepartureTime = hold.Service.DepartureTime,
                    ArrivalTime = hold.Service.ArrivalTime,
                    SeatNumbers = seatNumbersList,
                    TotalFareAmount = totalFare,
                    Status = "Confirmed",
                    ConfirmedAt = DateTime.UtcNow,
                    TicketId = ticket.Id,
                    QrCodePayload = qrPayload
                };
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    public async Task<List<HistoricalBookingDto>> GetPassengerBookingsAsync(Guid? passengerId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Bookings
            .Include(b => b.Passenger)
            .Include(b => b.Service)
                .ThenInclude(s => s.Route)
            .Include(b => b.Ticket)
            .Include(b => b.Refunds)
            .AsQueryable();

        if (_currentUserService != null && _currentUserService.IsAuthenticated && !_currentUserService.IsStaff)
        {
            var currentUserId = _currentUserService.UserId;
            var passengerProfile = await _context.PassengerProfiles
                .FirstOrDefaultAsync(p => p.UserId == currentUserId, cancellationToken);

            if (passengerProfile == null)
            {
                return new List<HistoricalBookingDto>();
            }

            // BOLA enforcement: passenger cannot query another passenger's bookings
            if (passengerId.HasValue && passengerId.Value != passengerProfile.Id && passengerId.Value != passengerProfile.UserId)
            {
                throw new UnauthorizedAccessException("You do not have permission to view bookings for another passenger.");
            }

            query = query.Where(b => b.PassengerId == passengerProfile.Id);
        }
        else if (passengerId.HasValue)
        {
            query = query.Where(b => b.PassengerId == passengerId.Value || b.Passenger.UserId == passengerId.Value);
        }

        var bookings = await query
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);

        return bookings.Select(MapToHistoricalBookingDto).ToList();
    }

    /// <inheritdoc />
    public async Task<PaginatedResponseDto<HistoricalBookingDto>> GetPassengerBookingsPaginatedAsync(
        Guid? passengerId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Bookings
            .Include(b => b.Passenger)
            .Include(b => b.Service)
                .ThenInclude(s => s.Route)
            .Include(b => b.Ticket)
            .Include(b => b.Refunds)
            .AsQueryable();

        if (_currentUserService != null && _currentUserService.IsAuthenticated && !_currentUserService.IsStaff)
        {
            var currentUserId = _currentUserService.UserId;
            var passengerProfile = await _context.PassengerProfiles
                .FirstOrDefaultAsync(p => p.UserId == currentUserId, cancellationToken);

            if (passengerProfile == null)
            {
                return new PaginatedResponseDto<HistoricalBookingDto>(new List<HistoricalBookingDto>(), 0, pageNumber, pageSize);
            }

            if (passengerId.HasValue && passengerId.Value != passengerProfile.Id && passengerId.Value != passengerProfile.UserId)
            {
                throw new UnauthorizedAccessException("You do not have permission to view bookings for another passenger.");
            }

            query = query.Where(b => b.PassengerId == passengerProfile.Id);
        }
        else if (passengerId.HasValue)
        {
            query = query.Where(b => b.PassengerId == passengerId.Value || b.Passenger.UserId == passengerId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var bookings = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = bookings.Select(MapToHistoricalBookingDto).ToList();

        return new PaginatedResponseDto<HistoricalBookingDto>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<HistoricalBookingDto?> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await _context.Bookings
            .Include(b => b.Passenger)
            .Include(b => b.Service)
                .ThenInclude(s => s.Route)
            .Include(b => b.Ticket)
            .Include(b => b.Refunds)
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

        if (booking == null) return null;

        // BOLA / Ownership enforcement: caller must own the booking unless staff
        if (_currentUserService != null && _currentUserService.IsAuthenticated && !_currentUserService.IsStaff)
        {
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue || booking.Passenger?.UserId != currentUserId.Value)
            {
                throw new UnauthorizedAccessException("You do not have permission to access this booking.");
            }
        }

        return MapToHistoricalBookingDto(booking);
    }

    public async Task<HistoricalBookingDto?> GetBookingByReferenceAsync(string bookingReference, CancellationToken cancellationToken = default)
    {
        var booking = await _context.Bookings
            .Include(b => b.Passenger)
            .Include(b => b.Service)
                .ThenInclude(s => s.Route)
            .Include(b => b.Ticket)
            .Include(b => b.Refunds)
            .FirstOrDefaultAsync(b => b.BookingReference == bookingReference, cancellationToken);

        if (booking == null) return null;

        // BOLA / Ownership enforcement: caller must own the booking unless staff
        if (_currentUserService != null && _currentUserService.IsAuthenticated && !_currentUserService.IsStaff)
        {
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue || booking.Passenger?.UserId != currentUserId.Value)
            {
                throw new UnauthorizedAccessException("You do not have permission to access this booking.");
            }
        }

        return MapToHistoricalBookingDto(booking);
    }

    public async Task<RefundResponseDto> CancelBookingAndRefundAsync(CancelBookingRequestDto request, CancellationToken cancellationToken = default)
    {
        var booking = await _context.Bookings
            .Include(b => b.Passenger)
            .Include(b => b.Service)
            .Include(b => b.Ticket)
            .FirstOrDefaultAsync(b => b.BookingReference == request.BookingReference, cancellationToken);

        if (booking == null)
        {
            throw new KeyNotFoundException($"Booking reference '{request.BookingReference}' not found.");
        }

        // BOLA / Ownership enforcement: caller must own the booking unless staff
        if (_currentUserService != null && _currentUserService.IsAuthenticated && !_currentUserService.IsStaff)
        {
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue || booking.Passenger?.UserId != currentUserId.Value)
            {
                throw new UnauthorizedAccessException("You do not have permission to cancel this booking.");
            }
        }

        if (booking.Status == BookingStatus.Cancelled)
        {
            throw new InvalidOperationException($"Booking '{request.BookingReference}' is already cancelled.");
        }

        // Apply BR-REFUND-001 Deterministic Tiered Cancellation Policy
        var departureTime = booking.Service?.DepartureTime ?? DateTime.UtcNow;
        var hoursRemaining = (departureTime - DateTime.UtcNow).TotalHours;

        decimal refundPercent;
        if (hoursRemaining > 24)
        {
            refundPercent = 0.90m; // Tier 1: 90% refund (10% fee retained)
        }
        else if (hoursRemaining >= 12)
        {
            refundPercent = 0.50m; // Tier 2: 50% refund (50% fee retained)
        }
        else
        {
            refundPercent = 0.0m;  // Tier 3: 0% non-refundable
        }

        var refundAmount = booking.TotalFareAmount * refundPercent;
        var cancellationFee = booking.TotalFareAmount - refundAmount;

        // Mutate status and persist refund record
        booking.Status = BookingStatus.Cancelled;

        if (booking.Ticket != null)
        {
            booking.Ticket.Status = TicketStatus.Cancelled;
        }

        // Release seat allocations so seats become available again
        var bookingAllocations = await _context.SeatAllocations
            .Where(sa => sa.BookingId == booking.Id)
            .ToListAsync(cancellationToken);

        if (bookingAllocations.Count > 0)
        {
            _context.SeatAllocations.RemoveRange(bookingAllocations);
        }

        var refund = new Refund
        {
            BookingId = booking.Id,
            RefundAmount = refundAmount,
            Percentage = refundPercent,
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? "Change of travel plans" : request.Reason,
            ProcessedAt = DateTime.UtcNow
        };

        await _context.Refunds.AddAsync(refund, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new RefundResponseDto
        {
            BookingReference = booking.BookingReference,
            OriginalFarePaid = booking.TotalFareAmount,
            RefundAmount = refundAmount,
            RefundPercentage = refundPercent,
            CancellationFee = cancellationFee,
            Reason = refund.Reason,
            Status = "Cancelled",
            ProcessedAt = refund.ProcessedAt
        };
    }

    public Task<string> GenerateTicketPayloadAsync(string bookingReference, string serviceCode, string seatNumbers, string passengerName)
    {
        var secret = _configuration["Jwt:Secret"] ?? "WayPoint_Super_Secret_Key_For_Jwt_Token_Authentication_2026_SE3090";
        var rawMessage = $"WP|REF:{bookingReference}|SRV:{serviceCode}|SEATS:{seatNumbers}|PASS:{passengerName}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawMessage));
        var hexSignature = Convert.ToHexString(hashBytes).ToLowerInvariant()[..16];

        return Task.FromResult($"{rawMessage}|HMAC:{hexSignature}");
    }

    public async Task<VerifyQrResponseDto> VerifyTicketQrAsync(VerifyQrRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.QrCodePayload))
        {
            return new VerifyQrResponseDto
            {
                IsValid = false,
                Status = "Invalid",
                Message = "QR payload cannot be empty."
            };
        }

        var parts = request.QrCodePayload.Split('|');
        if (parts.Length < 6 || parts[0] != "WP")
        {
            return new VerifyQrResponseDto
            {
                IsValid = false,
                Status = "Invalid Format",
                Message = "Tampered or unrecognized QR code format."
            };
        }

        string refCode = "", srvCode = "", seats = "", passName = "", hmacSig = "";
        foreach (var part in parts.Skip(1))
        {
            if (part.StartsWith("REF:")) refCode = part["REF:".Length..];
            else if (part.StartsWith("SRV:")) srvCode = part["SRV:".Length..];
            else if (part.StartsWith("SEATS:")) seats = part["SEATS:".Length..];
            else if (part.StartsWith("PASS:")) passName = part["PASS:".Length..];
            else if (part.StartsWith("HMAC:")) hmacSig = part["HMAC:".Length..];
        }

        // Cryptographic HMAC Verification
        var expectedPayload = await GenerateTicketPayloadAsync(refCode, srvCode, seats, passName);
        var expectedHmac = expectedPayload.Split('|').Last()["HMAC:".Length..];

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(hmacSig.ToLowerInvariant()),
                Encoding.UTF8.GetBytes(expectedHmac.ToLowerInvariant())))
        {
            return new VerifyQrResponseDto
            {
                IsValid = false,
                Status = "Security Violation: Tampered Signature",
                BookingReference = refCode,
                Message = "Digital signature failed HMAC verification. Potential ticket forgery detected."
            };
        }

        // If context is null (standalone unit testing), return cryptographic pass
        if (_context == null)
        {
            return new VerifyQrResponseDto
            {
                IsValid = true,
                Status = "Valid - Cryptographically Verified",
                BookingReference = refCode,
                ServiceCode = srvCode,
                PassengerName = passName,
                SeatNumbers = seats.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                Message = "HMAC signature verified successfully."
            };
        }

        // Lookup ticket in database
        var ticket = await _context.Tickets
            .Include(t => t.Booking)
                .ThenInclude(b => b.Service)
                    .ThenInclude(s => s.Route)
            .FirstOrDefaultAsync(t => t.Booking.BookingReference == refCode, cancellationToken);

        if (ticket == null)
        {
            return new VerifyQrResponseDto
            {
                IsValid = false,
                Status = "Ticket Not Found",
                BookingReference = refCode,
                Message = "No matching active ticket found in database."
            };
        }

        if (ticket.Status == TicketStatus.Cancelled)
        {
            return new VerifyQrResponseDto
            {
                IsValid = false,
                Status = "Cancelled / Refunded",
                BookingReference = refCode,
                Message = "This booking was cancelled and refunded. Boarding is denied."
            };
        }

        if (ticket.IsBoarded)
        {
            return new VerifyQrResponseDto
            {
                IsValid = true,
                Status = "Already Boarded",
                BookingReference = refCode,
                ServiceCode = srvCode,
                PassengerName = passName,
                SeatNumbers = seats.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                IsBoarded = true,
                BoardedAt = ticket.BoardedAt,
                Message = $"Passenger was already scanned and boarded at {ticket.BoardedAt:hh:mm tt}."
            };
        }

        // Mark as boarded
        ticket.IsBoarded = true;
        ticket.BoardedAt = DateTime.UtcNow;
        ticket.Status = TicketStatus.Boarded;
        await _context.SaveChangesAsync(cancellationToken);

        return new VerifyQrResponseDto
        {
            IsValid = true,
            Status = "Valid - Boarding Approved",
            BookingReference = refCode,
            ServiceCode = srvCode,
            RouteTitle = ticket.Booking.Service?.Route?.Name,
            PassengerName = passName,
            SeatNumbers = seats.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            IsBoarded = true,
            BoardedAt = ticket.BoardedAt,
            Message = "Valid e-ticket. Passenger cleared for immediate boarding."
        };
    }

    /// <inheritdoc />
    public async Task<TicketDetailsDto?> GetTicketByIdAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Booking)
                .ThenInclude(b => b.Passenger)
            .Include(t => t.Booking)
                .ThenInclude(b => b.Service)
                    .ThenInclude(s => s.Route)
            .FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken);

        if (ticket == null)
            return null;

        // BOLA / Ownership enforcement: caller must own the ticket unless staff
        if (_currentUserService != null && _currentUserService.IsAuthenticated && !_currentUserService.IsStaff)
        {
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue || ticket.Booking.Passenger?.UserId != currentUserId.Value)
            {
                throw new UnauthorizedAccessException("You do not have permission to access this ticket.");
            }
        }

        var route = ticket.Booking.Service?.Route;

        return new TicketDetailsDto
        {
            TicketId = ticket.Id,
            BookingReference = ticket.Booking.BookingReference,
            ServiceCode = ticket.Booking.Service?.ServiceCode,
            RouteTitle = route != null ? $"{route.OriginCity} - {route.DestinationCity}" : "Intercity Express",
            SeatNumbers = ticket.Booking.SeatNumbers.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            TotalFare = ticket.Booking.TotalFareAmount,
            Status = ticket.Status.ToString(),
            IsBoarded = ticket.IsBoarded,
            BoardedAt = ticket.BoardedAt,
            QrCodePayload = ticket.QrCodePayload,
            IssuedAt = ticket.CreatedAt,
            PassengerUserId = ticket.Booking.Passenger?.UserId
        };
    }

    private static HistoricalBookingDto MapToHistoricalBookingDto(Booking b)
    {
        var route = b.Service?.Route;
        var departureTime = b.Service?.DepartureTime ?? DateTime.UtcNow;
        var hoursUntilDeparture = Math.Max(0, (int)(departureTime - DateTime.UtcNow).TotalHours);

        // BR-REFUND-001 Policy Calculation
        decimal eligibleRefundPercent;
        if (hoursUntilDeparture > 24) eligibleRefundPercent = 0.90m;
        else if (hoursUntilDeparture >= 12) eligibleRefundPercent = 0.50m;
        else eligibleRefundPercent = 0.0m;

        var latestRefund = b.Refunds?.OrderByDescending(r => r.ProcessedAt).FirstOrDefault();

        return new HistoricalBookingDto
        {
            BookingId = b.Id,
            BookingReference = b.BookingReference,
            ServiceId = b.ServiceId,
            ServiceCode = b.Service?.ServiceCode ?? "SRV-UNKNOWN",
            RouteTitle = route != null ? $"{route.OriginCity} - {route.DestinationCity} ({route.Name})" : "Intercity Transit Corridor",
            OriginCity = route?.OriginCity ?? "Makumbura MMC (Colombo)",
            DestinationCity = route?.DestinationCity ?? "Destination Terminal",
            DepartureTime = departureTime,
            ArrivalTime = b.Service?.ArrivalTime ?? departureTime.AddHours(4),
            SeatNumbers = b.SeatNumbers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
            TotalFareAmount = b.TotalFareAmount,
            Status = b.Status.ToString(),
            BookedAt = b.CreatedAt,
            TicketId = b.Ticket?.Id,
            QrCodePayload = b.Ticket?.QrCodePayload,
            IsBoarded = b.Ticket?.IsBoarded ?? false,
            RefundAmount = latestRefund?.RefundAmount,
            RefundPercentage = latestRefund?.Percentage,
            CancellationReason = latestRefund?.Reason,
            HoursUntilDeparture = hoursUntilDeparture,
            EligibleRefundPercentage = eligibleRefundPercent
        };
    }
}
