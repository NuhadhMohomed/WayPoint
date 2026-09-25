using Microsoft.EntityFrameworkCore;
using WayPoint.Application.Common.Interfaces;
using WayPoint.Application.Features.JourneyPlanning;
using WayPoint.Application.Features.JourneyPlanning.DTOs;
using WayPoint.Domain.Entities.Journey;
using WayPoint.Domain.Enums;

namespace WayPoint.Infrastructure.Services;

public sealed class JourneyPlanningService : IJourneyPlanningService
{
    private const int MinimumTransferMinutes = 20;
    private readonly IWayPointDbContext context;

    public JourneyPlanningService(IWayPointDbContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyList<RouteDto>> GetRoutesAsync(string? origin, string? destination, bool activeOnly, CancellationToken cancellationToken)
    {
        var query = context.Routes.AsNoTracking().Include(route => route.Stops).AsQueryable();
        if (activeOnly) query = query.Where(route => route.IsActive);
        if (!string.IsNullOrWhiteSpace(origin)) query = query.Where(route => route.OriginCity.ToLower() == origin.Trim().ToLower());
        if (!string.IsNullOrWhiteSpace(destination)) query = query.Where(route => route.DestinationCity.ToLower() == destination.Trim().ToLower());
        var routes = await query.OrderBy(route => route.RouteCode).ToListAsync(cancellationToken);
        return routes.Select(ToRouteDto).ToList();
    }

    public async Task<RouteDetailsDto?> GetRouteAsync(Guid id, CancellationToken cancellationToken)
    {
        var route = await context.Routes.AsNoTracking().Include(item => item.Stops).Include(item => item.BoardingPoints).Include(item => item.TouristDestinations).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (route is null) return null;
        return new RouteDetailsDto
        {
            Id = route.Id, RouteNumber = route.RouteCode, OriginCity = route.OriginCity, DestinationCity = route.DestinationCity,
            EstimatedDurationMinutes = route.Stops.Select(stop => (int?)stop.ArrivalOffsetMinutes).Max() ?? 0, IsActive = route.IsActive,
            Stops = route.Stops.OrderBy(stop => stop.SequenceOrder).Select(ToStopDto).ToList(),
            BoardingPoints = route.BoardingPoints.Select(point => new BoardingPointDto { Id = point.Id, PointName = point.PointName, Landmark = point.Landmark, Latitude = point.Latitude, Longitude = point.Longitude }).ToList(),
            TouristDestinations = route.TouristDestinations.Select(destination => new TouristDestinationDto { Id = destination.Id, AttractionName = destination.AttractionName, Description = destination.Description, ImageUrl = destination.ImageUrl }).ToList()
        };
    }

    public async Task<RouteDto> CreateRouteAsync(CreateRouteDto request, CancellationToken cancellationToken)
    {
        var route = new Route
        {
            RouteCode = request.RouteNumber.Trim(), Name = $"{request.OriginCity.Trim()} - {request.DestinationCity.Trim()}",
            OriginCity = request.OriginCity.Trim(), DestinationCity = request.DestinationCity.Trim(),
            TotalDistanceKm = request.Stops.Select(stop => stop.DistanceFromOriginKm).DefaultIfEmpty().Max(), IsActive = true,
            Stops = request.Stops.Select(stop => new RouteStop { StopName = stop.StopName.Trim(), SequenceOrder = stop.SequenceOrder, ArrivalOffsetMinutes = stop.ArrivalOffsetMinutes, DistanceFromOriginKm = stop.DistanceFromOriginKm }).ToList()
        };
        await context.Routes.AddAsync(route, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return ToRouteDto(route);
    }

    public async Task<IReadOnlyList<ServiceDto>> GetServicesAsync(DateTime? date, Guid? routeId, CancellationToken cancellationToken)
    {
        var query = context.Services.AsNoTracking().Include(service => service.Route).AsQueryable();
        if (routeId.HasValue) query = query.Where(service => service.RouteId == routeId.Value);
        if (date.HasValue) query = query.Where(service => service.DepartureTime.Date == date.Value.Date);
        return await query.OrderBy(service => service.DepartureTime).Select(service => new ServiceDto { Id = service.Id, ServiceCode = service.ServiceCode, RouteId = service.RouteId, RouteNumber = service.Route.RouteCode, DepartureTime = service.DepartureTime, ArrivalTime = service.ArrivalTime, BaseFare = service.BaseFare, Status = service.Status.ToString() }).ToListAsync(cancellationToken);
    }

    public async Task<ServiceDto?> GetServiceAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.Services.AsNoTracking().Include(service => service.Route).Where(service => service.Id == id).Select(service => new ServiceDto { Id = service.Id, ServiceCode = service.ServiceCode, RouteId = service.RouteId, RouteNumber = service.Route.RouteCode, DepartureTime = service.DepartureTime, ArrivalTime = service.ArrivalTime, BaseFare = service.BaseFare, Status = service.Status.ToString() }).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<JourneySearchResponseDto> SearchAsync(JourneySearchRequestDto request, Guid? passengerId, CancellationToken cancellationToken)
    {
        var origin = request.OriginCity.Trim();
        var destination = request.DestinationCity.Trim();
        Guid? passengerProfileId = null;
        if (passengerId.HasValue)
        {
            passengerProfileId = await context.PassengerProfiles.Where(profile => profile.UserId == passengerId.Value).Select(profile => (Guid?)profile.Id).FirstOrDefaultAsync(cancellationToken);
        }
        var search = new JourneySearch { PassengerId = passengerProfileId, OriginCity = origin, DestinationCity = destination, TravelDate = request.TravelDate.Date, PassengerCount = request.PassengerCount };
        var services = await context.Services.AsNoTracking().Include(service => service.Route).ThenInclude(route => route.Stops).Include(service => service.Bus).Where(service => service.Status == ServiceStatus.Scheduled && service.DepartureTime.Date == request.TravelDate.Date).ToListAsync(cancellationToken);
        var candidates = services.Where(service => Matches(service.Route, origin, destination)).Select(service => ToCandidate(service, origin, destination, false)).ToList();
        if (!request.DirectOnly)
        {
            candidates.AddRange(services.Where(first => MatchesOrigin(first.Route, origin)).SelectMany(first => services.Where(second => MatchesDestination(second.Route, destination) && first.Id != second.Id && first.Route.DestinationCity.Equals(second.Route.OriginCity, StringComparison.OrdinalIgnoreCase) && first.ArrivalTime.AddMinutes(MinimumTransferMinutes) <= second.DepartureTime).Select(second =>
            {
                var candidate = ToCandidate(first, origin, first.Route.DestinationCity, true);
                candidate.TotalFare += second.BaseFare;
                candidate.ArrivalTime = second.ArrivalTime;
                candidate.TotalDurationMinutes = (int)(second.ArrivalTime - first.DepartureTime).TotalMinutes;
                candidate.MatchScore = 0.85m;
                return candidate;
            })));
        }
        var candidateServiceIds = candidates.Select(candidate => candidate.ServiceId).Distinct().ToList();
        var bookedSeats = await context.Bookings.AsNoTracking().Where(booking => candidateServiceIds.Contains(booking.ServiceId) && booking.Status != BookingStatus.Cancelled).Select(booking => new { booking.ServiceId, booking.SeatNumbers }).ToListAsync(cancellationToken);
        var heldSeats = await context.SeatHolds.AsNoTracking().Where(hold => candidateServiceIds.Contains(hold.ServiceId) && hold.Status == SeatHoldStatus.Held && hold.HeldUntil > DateTime.UtcNow).Select(hold => hold.ServiceId).ToListAsync(cancellationToken);
        foreach (var candidate in candidates)
        {
            var bookedCount = bookedSeats.Where(seat => seat.ServiceId == candidate.ServiceId).Sum(seat => seat.SeatNumbers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length);
            var heldCount = heldSeats.Count(serviceId => serviceId == candidate.ServiceId);
            var service = services.FirstOrDefault(item => item.Id == candidate.ServiceId);
            candidate.AvailableSeatsCount = Math.Max(0, (service?.Bus.TotalSeatCapacity ?? 0) - bookedCount - heldCount);
        }
        candidates = candidates.OrderByDescending(candidate => candidate.MatchScore).ThenBy(candidate => candidate.DepartureTime).Take(20).ToList();
        search.Candidates = candidates.Select(candidate => new JourneyCandidate { CandidateType = candidate.IsConnecting ? "Connecting" : "Direct", TotalFare = candidate.TotalFare, TotalDurationMinutes = candidate.TotalDurationMinutes, MatchScore = candidate.MatchScore }).ToList();
        await context.JourneySearches.AddAsync(search, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return new JourneySearchResponseDto { SearchId = search.Id, Candidates = candidates };
    }

    private static bool Matches(Route route, string origin, string destination) => MatchesOrigin(route, origin) && MatchesDestination(route, destination);
    private static bool MatchesOrigin(Route route, string origin) => route.OriginCity.Equals(origin, StringComparison.OrdinalIgnoreCase) || route.Stops.Any(stop => stop.StopName.Equals(origin, StringComparison.OrdinalIgnoreCase));
    private static bool MatchesDestination(Route route, string destination) => route.DestinationCity.Equals(destination, StringComparison.OrdinalIgnoreCase) || route.Stops.Any(stop => stop.StopName.Equals(destination, StringComparison.OrdinalIgnoreCase));

    private static CandidateJourneyDto ToCandidate(Service service, string origin, string destination, bool connecting)
    {
        var stops = service.Route.Stops.OrderBy(stop => stop.SequenceOrder).ToList();
        var start = stops.FirstOrDefault(stop => stop.StopName.Equals(origin, StringComparison.OrdinalIgnoreCase))?.ArrivalOffsetMinutes ?? 0;
        var end = stops.FirstOrDefault(stop => stop.StopName.Equals(destination, StringComparison.OrdinalIgnoreCase))?.ArrivalOffsetMinutes ?? (int)(service.ArrivalTime - service.DepartureTime).TotalMinutes;
        return new CandidateJourneyDto { ServiceId = service.Id, ServiceCode = service.ServiceCode, RouteNumber = service.Route.RouteCode, OriginCity = origin, DestinationCity = destination, DepartureTime = service.DepartureTime.AddMinutes(start), ArrivalTime = service.DepartureTime.AddMinutes(end), TotalFare = service.BaseFare, TotalDurationMinutes = Math.Max(0, end - start), IsConnecting = connecting, MatchScore = connecting ? 0.85m : 1.0m };
    }

    private static RouteDto ToRouteDto(Route route) => new() { Id = route.Id, RouteNumber = route.RouteCode, OriginCity = route.OriginCity, DestinationCity = route.DestinationCity, EstimatedDurationMinutes = route.Stops.Select(stop => (int?)stop.ArrivalOffsetMinutes).Max() ?? 0, IsActive = route.IsActive, Stops = route.Stops.OrderBy(stop => stop.SequenceOrder).Select(ToStopDto).ToList() };
    private static RouteStopDto ToStopDto(RouteStop stop) => new() { Id = stop.Id, StopName = stop.StopName, SequenceOrder = stop.SequenceOrder, ArrivalOffsetMinutes = stop.ArrivalOffsetMinutes, DistanceFromOriginKm = stop.DistanceFromOriginKm };
}