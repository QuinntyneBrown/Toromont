using Microsoft.EntityFrameworkCore;
using ToromontFleetHub.Api.Data;

namespace ToromontFleetHub.Api.Services;

public class WorkOrderNumberGenerator : IWorkOrderNumberGenerator
{
    private readonly FleetHubDbContext _db;

    public WorkOrderNumberGenerator(FleetHubDbContext db)
    {
        _db = db;
    }

    public async Task<string> GenerateAsync(CancellationToken ct = default)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"WO-{year}-";

        var lastNumber = await _db.WorkOrders
            .Where(w => w.WorkOrderNumber.StartsWith(prefix))
            .OrderByDescending(w => w.WorkOrderNumber)
            .Select(w => w.WorkOrderNumber)
            .FirstOrDefaultAsync(ct);

        var nextSequence = 1;
        if (lastNumber != null)
        {
            var sequencePart = lastNumber.Replace(prefix, "");
            if (int.TryParse(sequencePart, out var lastSeq))
                nextSequence = lastSeq + 1;
        }

        return $"{prefix}{nextSequence:D3}";
    }
}
