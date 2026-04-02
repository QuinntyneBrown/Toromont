using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.DTOs;
using IronvaleFleetHub.Api.Models;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.WorkOrders.Queries;

public record GetWorkOrderListQuery(
    int Skip,
    int Take,
    string? Status,
    string? Sort,
    Guid? EquipmentId
) : IRequest<PaginatedResponse<WorkOrder>>;

public class GetWorkOrderListQueryHandler : IRequestHandler<GetWorkOrderListQuery, PaginatedResponse<WorkOrder>>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;

    public GetWorkOrderListQueryHandler(FleetHubDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PaginatedResponse<WorkOrder>> Handle(GetWorkOrderListQuery request, CancellationToken ct)
    {
        var query = _db.WorkOrders
            .Include(w => w.Equipment)
            .Include(w => w.AssignedTo)
            .Where(w => w.OrganizationId == _tenant.OrganizationId)
            .AsNoTracking();

        if (request.EquipmentId.HasValue)
            query = query.Where(w => w.EquipmentId == request.EquipmentId.Value);

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(w => w.Status == request.Status);

        if (!string.IsNullOrWhiteSpace(request.Sort))
        {
            var desc = request.Sort.StartsWith("-");
            var field = desc ? request.Sort[1..] : request.Sort;
            query = field.ToLowerInvariant() switch
            {
                "priority" => desc ? query.OrderByDescending(w => w.Priority) : query.OrderBy(w => w.Priority),
                "requesteddate" => desc ? query.OrderByDescending(w => w.RequestedDate) : query.OrderBy(w => w.RequestedDate),
                "status" => desc ? query.OrderByDescending(w => w.Status) : query.OrderBy(w => w.Status),
                _ => query.OrderByDescending(w => w.CreatedAt)
            };
        }
        else
        {
            query = query.OrderByDescending(w => w.CreatedAt);
        }

        var total = await query.CountAsync(ct);
        var items = await query.Skip(request.Skip).Take(request.Take).ToListAsync(ct);

        return new PaginatedResponse<WorkOrder>
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
