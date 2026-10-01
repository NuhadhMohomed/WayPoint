namespace WayPoint.Application.DTOs.Common;

/// <summary>
/// Shared pagination query parameters for collection endpoints (API §2).
/// Bind from [FromQuery] in controllers.
/// </summary>
public class PaginationParams
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 20;

    public int PageNumber { get; set; } = 1;

    private int _pageSize = DefaultPageSize;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : (value < 1 ? DefaultPageSize : value);
    }
}
