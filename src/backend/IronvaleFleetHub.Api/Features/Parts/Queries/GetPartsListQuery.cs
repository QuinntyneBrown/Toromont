using IronvaleFleetHub.Api.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.DTOs;
using IronvaleFleetHub.Api.Models;

namespace IronvaleFleetHub.Api.Features.Parts.Queries;

public record GetPartsListQuery(
    int Skip,
    int Take,
    string? Category,
    string? Compatibility,
    string? Availability,
    string? Search,
    string? Sort
) : IRequest<PaginatedResponse<Part>>;

public class GetPartsListQueryHandler : IRequestHandler<GetPartsListQuery, PaginatedResponse<Part>>
{
    private readonly FleetHubDbContext _db;

    public GetPartsListQueryHandler(FleetHubDbContext db) => _db = db;

    public async Task<PaginatedResponse<Part>> Handle(GetPartsListQuery request, CancellationToken ct)
    {
        var query = _db.Parts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(p =>
                p.Name.ToLower().Contains(request.Search.ToLower()) ||
                p.PartNumber.ToLower().Contains(request.Search.ToLower()) ||
                p.Description.ToLower().Contains(request.Search.ToLower()));

        if (!string.IsNullOrWhiteSpace(request.Category))
            query = query.Where(p => p.Category == request.Category);

        if (!string.IsNullOrWhiteSpace(request.Compatibility))
            query = query.Where(p => p.CompatibleModels.Contains(request.Compatibility));

        if (!string.IsNullOrWhiteSpace(request.Availability))
            query = query.Where(p => p.Availability == request.Availability);

        if (!string.IsNullOrWhiteSpace(request.Sort))
        {
            var desc = request.Sort.StartsWith("-");
            var field = desc ? request.Sort[1..] : request.Sort;
            query = field.ToLowerInvariant() switch
            {
                "name" => desc ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
                "price" => desc ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
                "partnumber" => desc ? query.OrderByDescending(p => p.PartNumber) : query.OrderBy(p => p.PartNumber),
                _ => query.OrderBy(p => p.Name)
            };
        }
        else
        {
            query = query.OrderBy(p => p.Name);
        }

        var total = await query.CountAsync(ct);
        var items = await query.Skip(request.Skip).Take(request.Take).ToListAsync(ct);

        return new PaginatedResponse<Part>
        {
            Items = items,
            Pagination = new PaginationInfo
            {
                Page = request.Skip / request.Take + 1,
                PageSize = request.Take,
                TotalItems = total,
                TotalPages = (int)Math.Ceiling(total / (double)request.Take)
            }
        };
    }
}
