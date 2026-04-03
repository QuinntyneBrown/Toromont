using IronvaleFleetHub.Api.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.Search.Queries;

public record GlobalSearchQuery(string Q) : IRequest<GlobalSearchResult>, ISkipValidation;

public class GlobalSearchResult
{
    public List<EquipmentSearchResult> Equipment { get; init; } = [];
    public List<PartSearchResult> Parts { get; init; } = [];
    public List<WorkOrderSearchResult> WorkOrders { get; init; } = [];
}

public record EquipmentSearchResult(Guid Id, string Name, string SerialNumber, string Make, string Model, string Status, string Category);
public record PartSearchResult(Guid Id, string Name, string PartNumber, string Category, decimal Price);
public record WorkOrderSearchResult(Guid Id, string WorkOrderNumber, string Description, string Status, string Priority, string? EquipmentName);

public class GlobalSearchQueryHandler : IRequestHandler<GlobalSearchQuery, GlobalSearchResult>
{
    private const int MaxResultsPerCategory = 5;

    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;

    public GlobalSearchQueryHandler(FleetHubDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<GlobalSearchResult> Handle(GlobalSearchQuery request, CancellationToken ct)
    {
        var q = request.Q.Trim();

        var equipmentTask = SearchEquipmentAsync(q, ct);
        var partsTask = SearchPartsAsync(q, ct);
        var workOrdersTask = SearchWorkOrdersAsync(q, ct);

        await Task.WhenAll(equipmentTask, partsTask, workOrdersTask);

        return new GlobalSearchResult
        {
            Equipment = await equipmentTask,
            Parts = await partsTask,
            WorkOrders = await workOrdersTask,
        };
    }

    private async Task<List<EquipmentSearchResult>> SearchEquipmentAsync(string q, CancellationToken ct) =>
        await _db.Equipment
            .Where(e =>
                e.Name.Contains(q) ||
                e.SerialNumber.Contains(q) ||
                e.Make.Contains(q) ||
                e.Model.Contains(q))
            .OrderBy(e => e.Name)
            .Take(MaxResultsPerCategory)
            .Select(e => new EquipmentSearchResult(e.Id, e.Name, e.SerialNumber, e.Make, e.Model, e.Status, e.Category))
            .ToListAsync(ct);

    private async Task<List<PartSearchResult>> SearchPartsAsync(string q, CancellationToken ct) =>
        await _db.Parts
            .Where(p =>
                p.Name.Contains(q) ||
                p.PartNumber.Contains(q) ||
                p.Description.Contains(q) ||
                p.Category.Contains(q))
            .OrderBy(p => p.Name)
            .Take(MaxResultsPerCategory)
            .Select(p => new PartSearchResult(p.Id, p.Name, p.PartNumber, p.Category, p.Price))
            .ToListAsync(ct);

    private async Task<List<WorkOrderSearchResult>> SearchWorkOrdersAsync(string q, CancellationToken ct) =>
        await _db.WorkOrders
            .Include(w => w.Equipment)
            .Where(w =>
                w.WorkOrderNumber.Contains(q) ||
                w.Description.Contains(q) ||
                w.ServiceType.Contains(q))
            .OrderByDescending(w => w.CreatedAt)
            .Take(MaxResultsPerCategory)
            .Select(w => new WorkOrderSearchResult(
                w.Id,
                w.WorkOrderNumber,
                w.Description,
                w.Status,
                w.Priority,
                w.Equipment != null ? w.Equipment.Name : null))
            .ToListAsync(ct);
}
