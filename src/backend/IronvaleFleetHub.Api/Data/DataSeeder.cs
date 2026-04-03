using IronvaleFleetHub.Api.Models;

namespace IronvaleFleetHub.Api.Data;

public static class DataSeeder
{
    // Fixed GUIDs for deterministic seeding
    private static readonly Guid Org1Id = Guid.Parse("a1b2c3d4-0001-0000-0000-000000000001");
    private static readonly Guid Org2Id = Guid.Parse("a1b2c3d4-0002-0000-0000-000000000002");

    private static readonly Guid User1Id = Guid.Parse("b1b2c3d4-0001-0000-0000-000000000001");
    private static readonly Guid User2Id = Guid.Parse("b1b2c3d4-0002-0000-0000-000000000002");
    private static readonly Guid User3Id = Guid.Parse("b1b2c3d4-0003-0000-0000-000000000003");
    private static readonly Guid User4Id = Guid.Parse("b1b2c3d4-0004-0000-0000-000000000004");
    private static readonly Guid User5Id = Guid.Parse("b1b2c3d4-0005-0000-0000-000000000005");
    private static readonly Guid User6Id = Guid.Parse("b1b2c3d4-0006-0000-0000-000000000006");

    public static async Task SeedAsync(FleetHubDbContext db)
    {
        if (db.Organizations.Any()) return;

        var now = DateTime.UtcNow;

        // --- Organizations ---
        var orgs = new List<Organization>
        {
            new() { Id = Org1Id, Name = "Northern Construction Ltd.", ContactEmail = "admin@northernconstruction.ca", IsActive = true, CreatedAt = now },
            new() { Id = Org2Id, Name = "Pacific Mining Corp.", ContactEmail = "ops@pacificmining.ca", IsActive = true, CreatedAt = now },
        };
        db.Organizations.AddRange(orgs);

        // --- Users ---
        var users = new List<User>
        {
            new() { Id = User1Id, EntraObjectId = "dev-admin-001", Email = "admin@northernconstruction.ca", DisplayName = "Sarah Chen", Role = "Admin", OrganizationId = Org1Id, IsActive = true, CreatedAt = now },
            new() { Id = User2Id, EntraObjectId = "dev-fm-001", Email = "fleet@northernconstruction.ca", DisplayName = "Mike Rodriguez", Role = "FleetManager", OrganizationId = Org1Id, IsActive = true, CreatedAt = now },
            new() { Id = User3Id, EntraObjectId = "dev-tech-001", Email = "tech@northernconstruction.ca", DisplayName = "James Wilson", Role = "Technician", OrganizationId = Org1Id, IsActive = true, CreatedAt = now },
            new() { Id = User4Id, EntraObjectId = "dev-op-001", Email = "operator@northernconstruction.ca", DisplayName = "Emily Park", Role = "Operator", OrganizationId = Org1Id, IsActive = true, CreatedAt = now },
            new() { Id = User5Id, EntraObjectId = "dev-admin-002", Email = "admin@pacificmining.ca", DisplayName = "David Kim", Role = "Admin", OrganizationId = Org2Id, IsActive = true, CreatedAt = now },
            new() { Id = User6Id, EntraObjectId = "dev-fm-002", Email = "fleet@pacificmining.ca", DisplayName = "Lisa Thompson", Role = "FleetManager", OrganizationId = Org2Id, IsActive = true, CreatedAt = now },
        };
        db.Users.AddRange(users);

        // --- UserOrganizations ---
        var userOrgs = new List<UserOrganization>
        {
            new() { Id = Guid.NewGuid(), UserId = User1Id, OrganizationId = Org1Id, IsActive = true, IsDefault = true, JoinedAt = now },
            new() { Id = Guid.NewGuid(), UserId = User2Id, OrganizationId = Org1Id, IsActive = true, IsDefault = true, JoinedAt = now },
            new() { Id = Guid.NewGuid(), UserId = User3Id, OrganizationId = Org1Id, IsActive = true, IsDefault = true, JoinedAt = now },
            new() { Id = Guid.NewGuid(), UserId = User4Id, OrganizationId = Org1Id, IsActive = true, IsDefault = true, JoinedAt = now },
            new() { Id = Guid.NewGuid(), UserId = User5Id, OrganizationId = Org2Id, IsActive = true, IsDefault = true, JoinedAt = now },
            new() { Id = Guid.NewGuid(), UserId = User6Id, OrganizationId = Org2Id, IsActive = true, IsDefault = true, JoinedAt = now },
        };
        db.UserOrganizations.AddRange(userOrgs);

        // --- Equipment ---
        var equipmentIds = Enumerable.Range(1, 10).Select(i => Guid.Parse($"c1b2c3d4-{i:D4}-0000-0000-000000000001")).ToArray();
        var equipment = new List<Equipment>
        {
            new() { Id = equipmentIds[0], OrganizationId = Org1Id, Name = "CAT 320 Excavator #1", Make = "CAT", Model = "320", Year = 2022, SerialNumber = "CAT320-2022-001", Category = "Excavator", Status = "Operational", Latitude = 43.65, Longitude = -79.38, Location = "Toronto, ON - Site A", CreatedAt = now, UpdatedAt = now },
            new() { Id = equipmentIds[1], OrganizationId = Org1Id, Name = "Komatsu PC210 Excavator", Make = "Komatsu", Model = "PC210", Year = 2021, SerialNumber = "KOM210-2021-001", Category = "Excavator", Status = "NeedsService", Latitude = 43.70, Longitude = -79.42, Location = "Toronto, ON - Site B", LastServiceDate = now.AddDays(-90), CreatedAt = now, UpdatedAt = now },
            new() { Id = equipmentIds[2], OrganizationId = Org1Id, Name = "Volvo L120H Loader", Make = "Volvo", Model = "L120H", Year = 2023, SerialNumber = "VOL120-2023-001", Category = "Loader", Status = "Operational", Latitude = 44.23, Longitude = -76.48, Location = "Kingston, ON", CreatedAt = now, UpdatedAt = now },
            new() { Id = equipmentIds[3], OrganizationId = Org1Id, Name = "CAT D6 Dozer", Make = "CAT", Model = "D6", Year = 2020, SerialNumber = "CATD6-2020-001", Category = "Dozer", Status = "Operational", Latitude = 43.25, Longitude = -79.87, Location = "Hamilton, ON", CreatedAt = now, UpdatedAt = now },
            new() { Id = equipmentIds[4], OrganizationId = Org1Id, Name = "Deere 410L Backhoe", Make = "Deere", Model = "410L", Year = 2022, SerialNumber = "JD410-2022-001", Category = "Loader", Status = "OutOfService", Latitude = 43.45, Longitude = -80.48, Location = "Kitchener, ON", CreatedAt = now, UpdatedAt = now },
            new() { Id = equipmentIds[5], OrganizationId = Org1Id, Name = "Hitachi ZX350 Excavator", Make = "Hitachi", Model = "ZX350", Year = 2021, SerialNumber = "HIT350-2021-001", Category = "Excavator", Status = "Idle", Latitude = 43.86, Longitude = -79.33, Location = "Markham, ON", CreatedAt = now, UpdatedAt = now },
            new() { Id = equipmentIds[6], OrganizationId = Org2Id, Name = "CAT 745 Truck", Make = "CAT", Model = "745", Year = 2023, SerialNumber = "CAT745-2023-001", Category = "Truck", Status = "Operational", Latitude = 49.28, Longitude = -123.12, Location = "Vancouver, BC - Mine Site", CreatedAt = now, UpdatedAt = now },
            new() { Id = equipmentIds[7], OrganizationId = Org2Id, Name = "Komatsu HD785 Truck", Make = "Komatsu", Model = "HD785", Year = 2022, SerialNumber = "KOM785-2022-001", Category = "Truck", Status = "Operational", Latitude = 49.30, Longitude = -123.10, Location = "Vancouver, BC - Mine Site", CreatedAt = now, UpdatedAt = now },
            new() { Id = equipmentIds[8], OrganizationId = Org2Id, Name = "Liebherr LTM 1300 Crane", Make = "Liebherr", Model = "LTM 1300", Year = 2021, SerialNumber = "LIE1300-2021-001", Category = "Crane", Status = "NeedsService", Latitude = 49.25, Longitude = -123.15, Location = "Vancouver, BC - Port", CreatedAt = now, UpdatedAt = now },
            new() { Id = equipmentIds[9], OrganizationId = Org2Id, Name = "Volvo EC480E Excavator", Make = "Volvo", Model = "EC480E", Year = 2023, SerialNumber = "VOL480-2023-001", Category = "Excavator", Status = "Operational", Latitude = 49.22, Longitude = -123.08, Location = "Vancouver, BC - Quarry", CreatedAt = now, UpdatedAt = now },
        };
        db.Equipment.AddRange(equipment);

        // --- Work Orders ---
        var workOrders = new List<WorkOrder>
        {
            new() { Id = Guid.NewGuid(), OrganizationId = Org1Id, WorkOrderNumber = "WO-2026-001", EquipmentId = equipmentIds[1], ServiceType = "Corrective", Priority = "High", Status = "Open", Description = "Hydraulic leak detected on boom cylinder", RequestedDate = now.AddDays(-2), AssignedToUserId = User3Id, CreatedAt = now },
            new() { Id = Guid.NewGuid(), OrganizationId = Org1Id, WorkOrderNumber = "WO-2026-002", EquipmentId = equipmentIds[0], ServiceType = "Preventive", Priority = "Medium", Status = "InProgress", Description = "500-hour scheduled maintenance", RequestedDate = now.AddDays(-5), AssignedToUserId = User3Id, CreatedAt = now },
            new() { Id = Guid.NewGuid(), OrganizationId = Org1Id, WorkOrderNumber = "WO-2026-003", EquipmentId = equipmentIds[4], ServiceType = "Emergency", Priority = "Critical", Status = "Open", Description = "Engine failure - unit non-operational", RequestedDate = now.AddDays(-1), CreatedAt = now },
            new() { Id = Guid.NewGuid(), OrganizationId = Org1Id, WorkOrderNumber = "WO-2026-004", EquipmentId = equipmentIds[3], ServiceType = "Preventive", Priority = "Low", Status = "Completed", Description = "Track tension adjustment and undercarriage inspection", RequestedDate = now.AddDays(-14), CompletedDate = now.AddDays(-10), AssignedToUserId = User3Id, CreatedAt = now },
            new() { Id = Guid.NewGuid(), OrganizationId = Org2Id, WorkOrderNumber = "WO-2026-005", EquipmentId = equipmentIds[8], ServiceType = "Corrective", Priority = "High", Status = "OnHold", Description = "Outrigger cylinder seal replacement - awaiting parts", RequestedDate = now.AddDays(-3), CreatedAt = now },
        };
        db.WorkOrders.AddRange(workOrders);

        // --- Parts ---
        var parts = new List<Part>
        {
            new() { Id = Guid.NewGuid(), PartNumber = "FIL-OIL-001", Name = "Engine Oil Filter", Description = "High-efficiency engine oil filter for CAT/Komatsu", Price = 45.99m, Category = "Filters", Availability = "InStock", CompatibleModels = "CAT 320, Komatsu PC210" },
            new() { Id = Guid.NewGuid(), PartNumber = "FIL-AIR-001", Name = "Primary Air Filter", Description = "Heavy-duty primary air cleaner element", Price = 89.50m, Category = "Filters", Availability = "InStock", CompatibleModels = "CAT 320, CAT D6, Komatsu PC210" },
            new() { Id = Guid.NewGuid(), PartNumber = "FIL-FUEL-001", Name = "Fuel Filter Assembly", Description = "Fuel water separator filter kit", Price = 62.75m, Category = "Filters", Availability = "InStock", CompatibleModels = "CAT 320, Volvo L120H" },
            new() { Id = Guid.NewGuid(), PartNumber = "FIL-HYD-001", Name = "Hydraulic Return Filter", Description = "Hydraulic system return line filter", Price = 120.00m, Category = "Filters", Availability = "LowStock", CompatibleModels = "CAT 320, Hitachi ZX350" },
            new() { Id = Guid.NewGuid(), PartNumber = "HYD-SEAL-001", Name = "Boom Cylinder Seal Kit", Description = "Complete seal kit for main boom cylinder", Price = 285.00m, Category = "Hydraulics", Availability = "InStock", CompatibleModels = "CAT 320, Komatsu PC210" },
            new() { Id = Guid.NewGuid(), PartNumber = "HYD-PUMP-001", Name = "Hydraulic Main Pump", Description = "Variable displacement hydraulic main pump", Price = 4500.00m, Category = "Hydraulics", Availability = "LowStock", CompatibleModels = "CAT 320" },
            new() { Id = Guid.NewGuid(), PartNumber = "HYD-HOSE-001", Name = "High-Pressure Hydraulic Hose", Description = "5000 PSI rated hydraulic hose, 1 meter", Price = 75.00m, Category = "Hydraulics", Availability = "InStock", CompatibleModels = "Universal" },
            new() { Id = Guid.NewGuid(), PartNumber = "ENG-TURBO-001", Name = "Turbocharger Assembly", Description = "OEM replacement turbocharger", Price = 3200.00m, Category = "Engine", Availability = "OutOfStock", CompatibleModels = "CAT 320, CAT D6" },
            new() { Id = Guid.NewGuid(), PartNumber = "ENG-INJ-001", Name = "Fuel Injector", Description = "Precision fuel injector, Tier 4 compliant", Price = 680.00m, Category = "Engine", Availability = "InStock", CompatibleModels = "CAT 320, CAT D6, CAT 745" },
            new() { Id = Guid.NewGuid(), PartNumber = "ENG-BELT-001", Name = "Serpentine Belt", Description = "Engine accessory drive belt", Price = 55.00m, Category = "Engine", Availability = "InStock", CompatibleModels = "Volvo L120H, Volvo EC480E" },
            new() { Id = Guid.NewGuid(), PartNumber = "ENG-COOL-001", Name = "Water Pump Assembly", Description = "Engine coolant water pump", Price = 450.00m, Category = "Engine", Availability = "InStock", CompatibleModels = "Komatsu PC210, Komatsu HD785" },
            new() { Id = Guid.NewGuid(), PartNumber = "ELC-ALT-001", Name = "Alternator 24V", Description = "24-volt heavy-duty alternator", Price = 520.00m, Category = "Electrical", Availability = "InStock", CompatibleModels = "CAT 320, CAT D6, CAT 745" },
            new() { Id = Guid.NewGuid(), PartNumber = "ELC-STRTR-001", Name = "Starter Motor", Description = "24V electric starter motor", Price = 780.00m, Category = "Electrical", Availability = "LowStock", CompatibleModels = "Komatsu PC210, Hitachi ZX350" },
            new() { Id = Guid.NewGuid(), PartNumber = "ELC-BAT-001", Name = "Battery 12V 200Ah", Description = "Heavy-duty 12V battery for construction equipment", Price = 320.00m, Category = "Electrical", Availability = "InStock", CompatibleModels = "Universal" },
            new() { Id = Guid.NewGuid(), PartNumber = "ELC-SENS-001", Name = "Temperature Sensor", Description = "Coolant temperature sensor assembly", Price = 95.00m, Category = "Electrical", Availability = "InStock", CompatibleModels = "Universal" },
            new() { Id = Guid.NewGuid(), PartNumber = "TRK-SHOE-001", Name = "Track Shoe Assembly", Description = "Triple-grouser track shoe, 600mm", Price = 185.00m, Category = "Tracks", Availability = "InStock", CompatibleModels = "CAT 320, Komatsu PC210, Hitachi ZX350" },
            new() { Id = Guid.NewGuid(), PartNumber = "TRK-LINK-001", Name = "Track Chain Link", Description = "Sealed and lubricated track chain link", Price = 125.00m, Category = "Tracks", Availability = "InStock", CompatibleModels = "CAT 320, CAT D6" },
            new() { Id = Guid.NewGuid(), PartNumber = "TRK-IDLR-001", Name = "Front Idler", Description = "Front idler assembly with brackets", Price = 1650.00m, Category = "Tracks", Availability = "LowStock", CompatibleModels = "CAT 320, Komatsu PC210" },
            new() { Id = Guid.NewGuid(), PartNumber = "TRK-ROLL-001", Name = "Track Roller", Description = "Double-flange bottom track roller", Price = 380.00m, Category = "Tracks", Availability = "InStock", CompatibleModels = "CAT 320, Hitachi ZX350" },
            new() { Id = Guid.NewGuid(), PartNumber = "TRK-SPRK-001", Name = "Drive Sprocket", Description = "Final drive sprocket segment set", Price = 890.00m, Category = "Tracks", Availability = "InStock", CompatibleModels = "CAT D6, Komatsu PC210" },
        };
        db.Parts.AddRange(parts);

        // --- Alerts ---
        var alerts = new List<Alert>
        {
            new() { Id = Guid.NewGuid(), OrganizationId = Org1Id, EquipmentId = equipmentIds[1], AlertType = "ServiceDue", Severity = "High", Message = "Hydraulic oil change overdue by 50 hours", Status = "Active", CreatedAt = now.AddHours(-6) },
            new() { Id = Guid.NewGuid(), OrganizationId = Org1Id, EquipmentId = equipmentIds[4], AlertType = "EngineFailure", Severity = "Critical", Message = "Engine temperature exceeded safe limit - unit shut down", Status = "Active", CreatedAt = now.AddHours(-2) },
            new() { Id = Guid.NewGuid(), OrganizationId = Org1Id, EquipmentId = equipmentIds[0], AlertType = "FuelLevel", Severity = "Medium", Message = "Fuel level below 15%", Status = "Acknowledged", CreatedAt = now.AddDays(-1), AcknowledgedAt = now.AddHours(-20) },
            new() { Id = Guid.NewGuid(), OrganizationId = Org2Id, EquipmentId = equipmentIds[8], AlertType = "ServiceDue", Severity = "High", Message = "Annual inspection overdue", Status = "Active", CreatedAt = now.AddDays(-3) },
            new() { Id = Guid.NewGuid(), OrganizationId = Org2Id, EquipmentId = equipmentIds[6], AlertType = "Geofence", Severity = "Low", Message = "Equipment exited designated work zone", Status = "Resolved", CreatedAt = now.AddDays(-5), ResolvedAt = now.AddDays(-5).AddHours(2) },
        };
        db.Alerts.AddRange(alerts);

        // --- Equipment Model Thresholds ---
        var thresholds = new List<EquipmentModelThreshold>
        {
            new() { Id = Guid.NewGuid(), EquipmentModel = "CAT 320", MaxTemperature = 105.0, MaxFuelConsumptionRate = 25.0, MaxOperatingHoursPerDay = 18.0 },
            new() { Id = Guid.NewGuid(), EquipmentModel = "Komatsu PC210", MaxTemperature = 100.0, MaxFuelConsumptionRate = 22.0, MaxOperatingHoursPerDay = 16.0 },
            new() { Id = Guid.NewGuid(), EquipmentModel = "Volvo L120H", MaxTemperature = 98.0, MaxFuelConsumptionRate = 20.0, MaxOperatingHoursPerDay = 16.0 },
            new() { Id = Guid.NewGuid(), EquipmentModel = "CAT D6", MaxTemperature = 110.0, MaxFuelConsumptionRate = 30.0, MaxOperatingHoursPerDay = 20.0 },
            new() { Id = Guid.NewGuid(), EquipmentModel = "CAT 745", MaxTemperature = 108.0, MaxFuelConsumptionRate = 45.0, MaxOperatingHoursPerDay = 20.0 },
        };
        db.EquipmentModelThresholds.AddRange(thresholds);

        // --- Telemetry Events (7 days of data for first 3 equipment) ---
        var telemetryEvents = new List<TelemetryEvent>();
        var rng = new Random(42);
        foreach (var eqIdx in new[] { 0, 1, 2 })
        {
            for (int day = 0; day < 7; day++)
            {
                var ts = now.AddDays(-day).AddHours(-rng.Next(0, 8));
                telemetryEvents.Add(new TelemetryEvent
                {
                    Id = Guid.NewGuid(),
                    EquipmentId = equipmentIds[eqIdx],
                    OrganizationId = eqIdx < 6 ? Org1Id : Org2Id,
                    EventType = "Reading",
                    Timestamp = ts,
                    EngineHours = 4500 + day * 8 + rng.Next(0, 5),
                    FuelLevel = 40 + rng.Next(0, 50),
                    Temperature = 80 + rng.Next(0, 30),
                    Latitude = 43.65 + rng.NextDouble() * 0.1,
                    Longitude = -79.38 + rng.NextDouble() * 0.1
                });
            }
        }
        db.TelemetryEvents.AddRange(telemetryEvents);

        // --- AI Predictions ---
        var predictions = new List<AIPrediction>
        {
            new() { Id = Guid.NewGuid(), OrganizationId = Org1Id, EquipmentId = equipmentIds[0], Component = "Hydraulic Pump", ConfidenceScore = 0.87, RecommendedAction = "Replace hydraulic pump seals within 14 days", Timeframe = "7-14 days", Priority = "High", GeneratedAt = now.AddDays(-1) },
            new() { Id = Guid.NewGuid(), OrganizationId = Org1Id, EquipmentId = equipmentIds[1], Component = "Track Assembly", ConfidenceScore = 0.72, RecommendedAction = "Inspect and adjust track tension", Timeframe = "14-30 days", Priority = "Medium", GeneratedAt = now.AddDays(-2) },
            new() { Id = Guid.NewGuid(), OrganizationId = Org1Id, EquipmentId = equipmentIds[3], Component = "Air Filter", ConfidenceScore = 0.45, RecommendedAction = "Replace air filter at next service", Timeframe = "30+ days", Priority = "Low", GeneratedAt = now.AddDays(-3) },
        };
        db.AIPredictions.AddRange(predictions);

        // --- Anomaly Detections ---
        var anomalies = new List<AnomalyDetection>
        {
            new() { Id = Guid.NewGuid(), OrganizationId = Org1Id, EquipmentId = equipmentIds[0], AnomalyType = "TemperatureAnomaly", MetricName = "Engine Temperature", ExpectedValue = 92.0, ActualValue = 108.5, DeviationSigma = 2.8, Severity = "High", DetectedAt = now.AddHours(-4) },
            new() { Id = Guid.NewGuid(), OrganizationId = Org1Id, EquipmentId = equipmentIds[2], AnomalyType = "FuelAnomaly", MetricName = "Fuel Consumption", ExpectedValue = 18.0, ActualValue = 26.5, DeviationSigma = 2.1, Severity = "Medium", DetectedAt = now.AddHours(-8) },
        };
        db.AnomalyDetections.AddRange(anomalies);

        // --- Notification Preferences (default for all Org1 users) ---
        var notifTypes = new[] { "CriticalAlert", "WorkOrderAssigned", "ServiceDue", "OrderConfirmation", "AnomalyDetected" };
        var prefs = new List<NotificationPreference>();
        foreach (var userId in new[] { User1Id, User2Id, User3Id, User4Id })
        {
            foreach (var nt in notifTypes)
            {
                prefs.Add(new NotificationPreference
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    NotificationType = nt,
                    EmailEnabled = nt is "CriticalAlert" or "WorkOrderAssigned" or "OrderConfirmation",
                    SmsEnabled = nt == "CriticalAlert"
                });
            }
        }
        db.NotificationPreferences.AddRange(prefs);

        // --- Notifications ---
        var notifications = new List<Notification>
        {
            new() { Id = Guid.NewGuid(), UserId = User1Id, OrganizationId = Org1Id, Type = "CriticalAlert", Title = "Engine Overheat", Message = "CAT 745 engine temperature exceeded safe limit", EntityType = "Equipment", EntityId = equipmentIds[4], IsRead = false, CreatedAt = now.AddHours(-2) },
            new() { Id = Guid.NewGuid(), UserId = User2Id, OrganizationId = Org1Id, Type = "WorkOrderAssigned", Title = "Work Order Assigned", Message = "WO-2026-001 has been assigned to you", EntityType = "WorkOrder", IsRead = false, CreatedAt = now.AddHours(-6) },
            new() { Id = Guid.NewGuid(), UserId = User1Id, OrganizationId = Org1Id, Type = "ServiceDue", Title = "Service Reminder", Message = "CAT 320 Excavator #1 - 500hr service due in 3 days", EntityType = "Equipment", EntityId = equipmentIds[0], IsRead = true, CreatedAt = now.AddDays(-1) },
        };
        db.Notifications.AddRange(notifications);

        // --- AI Scenario Records (Design 21) ---
        var aiScenarios = new List<AiScenarioRecord>
        {
            new() { Id = Guid.NewGuid(), EquipmentId = equipmentIds[0], ScenarioType = "Prediction", PredictedIssue = "Engine overheating risk", ConfidenceScore = 0.87m, RecommendedAction = "Inspect cooling system, check coolant levels and radiator condition within 7 days", Priority = "High", Explanation = "Temperature trending above threshold. Average readings indicate cooling system degradation. Immediate inspection recommended.", CreatedAt = now },
            new() { Id = Guid.NewGuid(), EquipmentId = equipmentIds[1], ScenarioType = "Prediction", PredictedIssue = "Hydraulic system maintenance required", ConfidenceScore = 0.72m, RecommendedAction = "Inspect and service hydraulic system within 14 days", Priority = "Medium", Explanation = "Equipment status indicates service needed. Fuel variance and operating patterns suggest hydraulic system wear.", CreatedAt = now },
            new() { Id = Guid.NewGuid(), EquipmentId = equipmentIds[2], ScenarioType = "Prediction", PredictedIssue = "General wear monitoring", ConfidenceScore = 0.45m, RecommendedAction = "Continue monitoring, schedule routine inspection", Priority = "Low", Explanation = "Limited telemetry data available. Confidence is low due to insufficient historical readings.", CreatedAt = now },
            new() { Id = Guid.NewGuid(), EquipmentId = equipmentIds[4], ScenarioType = "Prediction", PredictedIssue = "Equipment requires immediate maintenance", ConfidenceScore = 0.35m, RecommendedAction = "Schedule emergency maintenance and remove from service rotation", Priority = "Low", Explanation = "cold-start: fewer than 30 days of telemetry. Equipment is OutOfService with limited diagnostic data.", CreatedAt = now },
        };
        db.AiScenarioRecords.AddRange(aiScenarios);

        // --- Dev Parts Order Events (Design 19 - Workflow Engine) ---
        var partsOrder = db.PartsOrders.Local.FirstOrDefault();
        if (partsOrder == null)
        {
            partsOrder = new PartsOrder
            {
                Id = Guid.NewGuid(),
                OrganizationId = Org1Id,
                OrderNumber = "PO-2026-001",
                UserId = User2Id,
                Status = "Submitted",
                Subtotal = 4785.00m,
                CreatedAt = now.AddDays(-2)
            };
            db.PartsOrders.Add(partsOrder);
        }

        var partsOrderEvents = new List<DevPartsOrderEvent>
        {
            new() { Id = Guid.NewGuid(), PartsOrderId = partsOrder.Id, ExternalEventId = "EVT-001", NewStatus = "Processing", TrackingNumber = null, EventTimestamp = now.AddDays(-1), Processed = false },
            new() { Id = Guid.NewGuid(), PartsOrderId = partsOrder.Id, ExternalEventId = "EVT-002", NewStatus = "Shipped", TrackingNumber = "TRK-12345-CA", EventTimestamp = now.AddHours(-6), Processed = false },
        };
        db.DevPartsOrderEvents.AddRange(partsOrderEvents);

        await db.SaveChangesAsync();
    }
}
