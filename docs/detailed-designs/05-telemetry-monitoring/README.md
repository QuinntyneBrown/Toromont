# Telemetry & Monitoring — Detailed Design

## 1. Overview

This feature handles ingestion, processing, and visualization of real-time equipment telemetry data. Azure Functions provide a high-throughput ingestion endpoint (100+ events/sec). Telemetry data feeds interactive Kendo UI charts showing engine hours, fuel consumption, temperature, and GPS trails. Alert thresholds trigger critical notifications when readings exceed configured limits.

**Traces to:** L1-005, L1-015 | **L2:** L2-012, L2-013, L2-033

## 2. Component Details

### 2.1 Telemetry Ingestion Function (`TelemetryIngestionFunction`)
- **Trigger**: HTTP POST at `/api/telemetry`
- **Responsibility**: Validates event, enriches with equipment metadata, persists to SQL, triggers alert evaluation
- **Performance**: Handles 100+ events/second using Dapper bulk inserts (not EF Core)
- **Retry Policy**: 3 retries with exponential backoff (1s, 4s, 16s) on transient DB errors
- **Dead Letter**: Events that fail after 3 retries stored in `TelemetryDeadLetterQueue` table

### 2.2 Alert Evaluator (`AlertEvaluatorService`)
- **Responsibility**: Compares telemetry readings against configured thresholds
- **Threshold Types**: Per-equipment custom thresholds, or global defaults by equipment model
- **Alert Generation**: Creates Alert record, triggers notification pipeline via SignalR + email

### 2.3 Telemetry Controller (`TelemetryController`)
- `GET /api/v1/equipment/{id}/telemetry?range=7d&metrics=engineHours,fuelConsumption,temperature`
- `GET /api/v1/equipment/{id}/telemetry/latest` — most recent readings
- `GET /api/v1/equipment/{id}/telemetry/gps-trail?range=7d` — GPS coordinate history
- **Performance**: Uses Dapper for time-series queries with date range partitioning

### 2.4 Alerts Controller (`AlertsController`)
- `GET /api/v1/alerts` — active alerts for org, sorted by severity then timestamp
- `PUT /api/v1/alerts/{id}/acknowledge` — mark alert as acknowledged
- `PUT /api/v1/alerts/{id}/resolve` — mark alert as resolved

### 2.5 Angular Telemetry Module
- **TelemetryDashboardComponent**: Equipment selector, time range toggle, 4 chart panels
- **Charts**: Kendo Line (engine hours, temperature), Kendo Bar (fuel), map (GPS trail)
- **Auto-refresh**: `interval(60000)` observable reloads chart data every 60 seconds
- **Responsive**: Charts stack vertically in single column below 768px

## 3. Data Model

### 3.1 Class Diagram
![Class Diagram](diagrams/class_diagram.png)

### 3.2 Key Indexes
- `IX_TelemetryEvents_EquipmentId_Timestamp` — time-range queries (most critical for performance)
- `IX_Alerts_OrganizationId_Status_Severity` — dashboard alerts panel
- **Partitioning**: Consider table partitioning by month on `TelemetryEvents` for large datasets

## 4. Key Workflows

### 4.1 Telemetry Event Ingestion
![Sequence Diagram](diagrams/sequence_telemetry.png)

## 5. API Contracts

### POST /api/telemetry (Azure Function)
```json
// Request
{
  "equipmentId": "guid",
  "timestamp": "2026-04-01T14:30:00Z",
  "eventType": "periodic_reading",
  "payload": {
    "engineHours": 4521.5,
    "fuelLevel": 73.2,
    "temperature": 185.4,
    "latitude": 43.7001,
    "longitude": -79.4163,
    "rpm": 1850
  }
}
// Response 202 Accepted
{ "eventId": "guid", "status": "accepted" }
```

## 6. Security Considerations
- Telemetry ingestion endpoint uses API key authentication (not user JWT) — equipment sends via service account
- Alert thresholds configurable only by Admin and Fleet Manager roles
- Telemetry data is tenant-filtered — users see only their organization's equipment data

## 7. Open Questions
1. Should telemetry data have a retention policy (e.g., raw data for 1 year, aggregated data for 5 years)?
2. Should the ingestion function use Azure Service Bus for buffering during traffic spikes?
