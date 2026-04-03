using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Features.Equipment.Commands;

public record ImportEquipmentResult(int Imported);

public record ImportEquipmentCommand(Stream FileStream) : IRequest<ImportEquipmentResult>;

public class ImportEquipmentCommandHandler : IRequestHandler<ImportEquipmentCommand, ImportEquipmentResult>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ILogger<ImportEquipmentCommandHandler> _logger;

    public ImportEquipmentCommandHandler(
        FleetHubDbContext db, ITenantContext tenant,
        ILogger<ImportEquipmentCommandHandler> logger)
    {
        _db = db;
        _tenant = tenant;
        _logger = logger;
    }

    public async Task<ImportEquipmentResult> Handle(ImportEquipmentCommand request, CancellationToken ct)
    {
        var doc = await System.Xml.Linq.XDocument.LoadAsync(
            request.FileStream, System.Xml.Linq.LoadOptions.None, ct);

        var upsertCount = 0;
        var elements = doc.Descendants("Equipment");

        foreach (var el in elements)
        {
            var serial = el.Element("SerialNumber")?.Value;
            if (string.IsNullOrWhiteSpace(serial)) continue;

            var existing = await _db.Equipment
                .FirstOrDefaultAsync(e => e.SerialNumber == serial && e.OrganizationId == _tenant.OrganizationId, ct);

            if (existing is not null)
            {
                existing.Name = el.Element("Name")?.Value ?? existing.Name;
                existing.Make = el.Element("Make")?.Value ?? existing.Make;
                existing.Model = el.Element("Model")?.Value ?? existing.Model;
                if (int.TryParse(el.Element("Year")?.Value, out var year)) existing.Year = year;
                existing.Category = el.Element("Category")?.Value ?? existing.Category;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _db.Equipment.Add(new Models.Equipment
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = _tenant.OrganizationId,
                    SerialNumber = serial,
                    Name = el.Element("Name")?.Value ?? string.Empty,
                    Make = el.Element("Make")?.Value ?? string.Empty,
                    Model = el.Element("Model")?.Value ?? string.Empty,
                    Year = int.TryParse(el.Element("Year")?.Value, out var y) ? y : 0,
                    Category = el.Element("Category")?.Value ?? string.Empty,
                    Location = el.Element("Location")?.Value ?? string.Empty,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            upsertCount++;
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Imported {Count} equipment records for org {OrgId}", upsertCount, _tenant.OrganizationId);

        return new ImportEquipmentResult(upsertCount);
    }
}
