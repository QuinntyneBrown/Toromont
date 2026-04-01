# AI Insights — Detailed Design

## 1. Overview

This feature provides AI-driven predictive maintenance recommendations and anomaly detection. An Azure Function runs scheduled analysis on historical telemetry data and service records to predict component failures before they occur. Real-time anomaly detection identifies unusual patterns in telemetry streams. Results are displayed on a dedicated AI Insights dashboard and on equipment detail pages.

**Traces to:** L1-006 | **L2:** L2-014, L2-015, L2-016

## 2. Component Details

### 2.1 Predictive Maintenance Function (`PredictiveMaintenanceFunction`)
- **Trigger**: Timer trigger — runs daily at 2:00 AM UTC
- **Process**: For each equipment with ≥30 days of telemetry data:
  1. Aggregates telemetry metrics (engine hours, temperature trends, fuel patterns)
  2. Correlates with past service history (what failed, when, under what conditions)
  3. Runs prediction model to estimate failure probability per component
  4. Generates/updates AIPrediction records with confidence scores
- **Model**: Azure ML deployed model endpoint, or rule-based heuristics as initial implementation

### 2.2 Anomaly Detection Service (`AnomalyDetectionService`)
- **Trigger**: Called from telemetry ingestion pipeline for every new event
- **Detection Methods**:
  - Temperature: >2σ deviation from 30-day rolling average (L2-016 AC1)
  - Fuel: >30% increase vs 7-day average without proportional hours increase (L2-016 AC2)
  - Operating Pattern: Irregular on/off cycles compared to equipment baseline
- **Output**: Creates Alert record (type=Anomaly) and triggers notification to Fleet Manager

### 2.3 AI Insights Controller (`AIInsightsController`)
- `GET /api/v1/ai/predictions` — all active predictions for org, sortable by confidence
- `GET /api/v1/ai/predictions?equipmentId=` — predictions for specific equipment
- `PUT /api/v1/ai/predictions/{id}/dismiss` — dismiss a prediction
- `GET /api/v1/ai/anomalies` — recent anomaly detections
- `GET /api/v1/ai/dashboard-stats` — KPI aggregates (total predictions, high priority count, estimated savings)

### 2.4 Angular AI Module
- **AIInsightsDashboardComponent**: 4 KPI cards, predictions Kendo Grid, anomaly alerts list
- **Confidence Visualization**: Progress bars with color coding (≥80%=red "High Priority", <50%=gray "Low Confidence")
- **Equipment Detail Integration**: AI Insights section injected into equipment detail page

## 3. Data Model

### 3.1 Class Diagram
![Class Diagram](diagrams/class_diagram.png)

## 4. API Contracts

### GET /api/v1/ai/predictions
```json
{
  "data": [{
    "id": "guid",
    "equipmentName": "CAT 320 GC Excavator",
    "component": "Hydraulic Pump",
    "confidenceScore": 0.86,
    "recommendedAction": "Schedule hydraulic pump inspection and seal replacement",
    "timeframe": "7-14 days",
    "priority": "High",
    "generatedAt": "2026-04-01T02:00:00Z"
  }],
  "pagination": { "page": 1, "pageSize": 20, "totalCount": 47 }
}
```

## 5. Security Considerations
- AI predictions are tenant-scoped — users see only their organization's predictions
- Prediction model does not expose raw telemetry data across tenants
- Anomaly detection thresholds (σ deviation) configurable only by Admin

## 6. Open Questions
1. Should the initial release use rule-based heuristics or invest in ML model training from day one?
2. How to handle the cold-start problem — new equipment with <30 days data?
3. What cost savings methodology to use for the "Estimated Cost Savings" KPI?
