using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Models;

namespace IronvaleFleetHub.Api.Services;

public class AlertEvaluatorService : IAlertEvaluatorService
{
    private readonly FleetHubDbContext _db;
    private readonly ILogger<AlertEvaluatorService> _logger;

    public AlertEvaluatorService(FleetHubDbContext db, ILogger<AlertEvaluatorService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task EvaluateAsync(TelemetryEvent telemetryEvent)
    {
        var equipment = await _db.Equipment
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == telemetryEvent.EquipmentId);

        if (equipment is null) return;

        var threshold = await _db.EquipmentModelThresholds
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.EquipmentModel == equipment.Make + " " + equipment.Model);

        if (threshold is null) return;

        // Temperature check
        if (telemetryEvent.Temperature > threshold.MaxTemperature)
        {
            var alert = new Alert
            {
                Id = Guid.NewGuid(),
                OrganizationId = equipment.OrganizationId,
                EquipmentId = equipment.Id,
                AlertType = "TemperatureExceeded",
                Severity = telemetryEvent.Temperature > threshold.MaxTemperature * 1.1 ? "Critical" : "High",
                Message = $"Temperature {telemetryEvent.Temperature:F1}C exceeds threshold {threshold.MaxTemperature:F1}C",
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
            };
            _db.Alerts.Add(alert);
            _logger.LogWarning("Temperature alert created for equipment {EquipmentId}: {Temp}C", equipment.Id, telemetryEvent.Temperature);
        }

        // Low fuel check
        if (telemetryEvent.FuelLevel < 10)
        {
            var alert = new Alert
            {
                Id = Guid.NewGuid(),
                OrganizationId = equipment.OrganizationId,
                EquipmentId = equipment.Id,
                AlertType = "FuelLevel",
                Severity = telemetryEvent.FuelLevel < 5 ? "High" : "Medium",
                Message = $"Fuel level critically low at {telemetryEvent.FuelLevel:F1}%",
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
            };
            _db.Alerts.Add(alert);
        }

        await _db.SaveChangesAsync();
    }
}
