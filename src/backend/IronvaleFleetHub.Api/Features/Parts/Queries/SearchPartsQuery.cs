using IronvaleFleetHub.Api.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Models;

namespace IronvaleFleetHub.Api.Features.Parts.Queries;

public record SearchPartsQuery(string Q) : IRequest<List<Part>>;

public class SearchPartsQueryHandler : IRequestHandler<SearchPartsQuery, List<Part>>
{
    private readonly FleetHubDbContext _db;

    public SearchPartsQueryHandler(FleetHubDbContext db) => _db = db;

    public async Task<List<Part>> Handle(SearchPartsQuery request, CancellationToken ct)
    {
        var tokens = request.Q.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var query = _db.Parts.AsNoTracking();

        foreach (var token in tokens)
        {
            var t = token;
            query = query.Where(p =>
                p.Name.ToLower().Contains(t) ||
                p.Description.ToLower().Contains(t) ||
                p.PartNumber.ToLower().Contains(t) ||
                p.Category.ToLower().Contains(t) ||
                p.CompatibleModels.ToLower().Contains(t));
        }

        return await query.Take(50).ToListAsync(ct);
    }
}
