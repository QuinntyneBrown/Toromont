using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Models;

namespace IronvaleFleetHub.Api.Features.Parts.Queries;

public record GetPartByIdQuery(Guid Id) : IRequest<Part?>;

public class GetPartByIdQueryHandler : IRequestHandler<GetPartByIdQuery, Part?>
{
    private readonly FleetHubDbContext _db;

    public GetPartByIdQueryHandler(FleetHubDbContext db) => _db = db;

    public async Task<Part?> Handle(GetPartByIdQuery request, CancellationToken ct)
    {
        return await _db.Parts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.Id, ct);
    }
}
