using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.DTOs;
using IronvaleFleetHub.Api.Models;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.Equipment.Queries;

public record GetEquipmentListQuery(
    int Skip,
    int Take,
    string? Status,
    string? Category,
    string? Search,
    string? Sort,
    string? Filter
) : IRequest<PaginatedResponse<Models.Equipment>>;

public class GetEquipmentListQueryHandler : IRequestHandler<GetEquipmentListQuery, PaginatedResponse<Models.Equipment>>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;

    public GetEquipmentListQueryHandler(FleetHubDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PaginatedResponse<Models.Equipment>> Handle(GetEquipmentListQuery request, CancellationToken ct)
    {
        var query = _db.Equipment
            .Where(e => e.OrganizationId == _tenant.OrganizationId)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(e => e.Status == request.Status);

        if (!string.IsNullOrWhiteSpace(request.Category))
            query = query.Where(e => e.Category == request.Category);

        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(e =>
                e.Name.Contains(request.Search) ||
                e.SerialNumber.Contains(request.Search) ||
                e.Make.Contains(request.Search) ||
                e.Model.Contains(request.Search));

        if (!string.IsNullOrWhiteSpace(request.Sort))
        {
            var desc = request.Sort.StartsWith("-");
            var field = desc ? request.Sort[1..] : request.Sort;
            query = field.ToLowerInvariant() switch
            {
                "name" => desc ? query.OrderByDescending(e => e.Name) : query.OrderBy(e => e.Name),
                "status" => desc ? query.OrderByDescending(e => e.Status) : query.OrderBy(e => e.Status),
                "createdat" => desc ? query.OrderByDescending(e => e.CreatedAt) : query.OrderBy(e => e.CreatedAt),
                _ => query.OrderByDescending(e => e.CreatedAt)
            };
        }
        else
        {
            query = query.OrderByDescending(e => e.CreatedAt);
        }

        var total = await query.CountAsync(ct);
        var items = await query.Skip(request.Skip).Take(request.Take).ToListAsync(ct);

        return new PaginatedResponse<Models.Equipment>
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
