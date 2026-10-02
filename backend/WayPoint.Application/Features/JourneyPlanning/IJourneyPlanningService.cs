using WayPoint.Application.DTOs.Common;
using WayPoint.Application.Features.JourneyPlanning.DTOs;

namespace WayPoint.Application.Features.JourneyPlanning;

public interface IJourneyPlanningService
{
    Task<IReadOnlyList<RouteDto>> GetRoutesAsync(string? origin, string? destination, bool activeOnly, CancellationToken cancellationToken);
    Task<RouteDetailsDto?> GetRouteAsync(Guid id, CancellationToken cancellationToken);
    Task<RouteDto> CreateRouteAsync(CreateRouteDto request, CancellationToken cancellationToken);
    Task<IReadOnlyList<ServiceDto>> GetServicesAsync(DateTime? date, Guid? routeId, string? status, CancellationToken cancellationToken);

    /// <summary>
    /// Paginated service listing (API §2 standardized pagination).
    /// </summary>
    Task<PaginatedResponseDto<ServiceDto>> GetServicesPaginatedAsync(DateTime? date, Guid? routeId, string? status, int pageNumber, int pageSize, CancellationToken cancellationToken);

    Task<ServiceDto?> GetServiceAsync(Guid id, CancellationToken cancellationToken);
    Task<JourneySearchResponseDto> SearchAsync(JourneySearchRequestDto request, Guid? passengerId, CancellationToken cancellationToken);
    Task<ServiceDto> CreateServiceAsync(CreateServiceDto request, CancellationToken cancellationToken);
}

public class RouteDetailsDto : RouteDto
{
    public List<BoardingPointDto> BoardingPoints { get; set; } = new();
    public List<TouristDestinationDto> TouristDestinations { get; set; } = new();
}

public class ServiceDto
{
    public Guid Id { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public Guid RouteId { get; set; }
    public string RouteNumber { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public decimal BaseFare { get; set; }
    public string Status { get; set; } = string.Empty;
}