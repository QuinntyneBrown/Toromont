# Service Management — Detailed Design

## 1. Overview

This feature enables scheduling, tracking, and managing equipment maintenance through work orders. Work orders follow a defined lifecycle (Open → In Progress → On Hold → Completed → Closed) with full audit history. A calendar view provides visual scheduling using Kendo UI Scheduler. Azure Logic Apps automate service reminders and work order escalation.

**Traces to:** L1-003, L1-015 | **L2:** L2-007, L2-008, L2-009, L2-034

**Actors:** Fleet Manager (create, close), Technician (create, update status), Operator (read-only)

## 2. Architecture

### 2.1 C4 Component Diagram
![C4 Component](diagrams/c4_component.png)

## 3. Component Details

### 3.1 WorkOrder Controller (`WorkOrdersController`)
- **Endpoints**:
  - `GET /api/v1/work-orders` — paginated list with status/priority/equipment filters
  - `GET /api/v1/work-orders/{id}` — detail with history timeline
  - `POST /api/v1/work-orders` — create (Fleet Manager, Technician)
  - `PUT /api/v1/work-orders/{id}/status` — transition status with notes
  - `GET /api/v1/work-orders/calendar?start=&end=` — calendar view data
- **Status Transition Rules**:
  - Open → InProgress (Technician, Fleet Manager)
  - InProgress → OnHold (Technician, Fleet Manager)
  - OnHold → InProgress (Technician, Fleet Manager)
  - InProgress → Completed (Technician, Fleet Manager)
  - Completed → Closed (Fleet Manager only)
  - Closed → no further transitions (read-only)

### 3.2 WorkOrder Service (MediatR Handlers)
- **CreateWorkOrderHandler**: Generates unique WO number (WO-YYYYMMDD-NNN), validates equipment exists in org, creates WO with Open status, records initial history entry, triggers notification to assigned technician.
- **UpdateWorkOrderStatusHandler**: Validates transition is allowed for current status + user role, records history entry with timestamp/user/notes, triggers relevant notifications.
- **GetCalendarDataHandler**: Returns work orders within date range formatted for Kendo Scheduler (title, start, end, color by priority).

### 3.3 Logic App Workflows
- **Escalation Workflow**: Runs hourly. Queries WOs in "Open" status for >48 hours. Escalates priority by one level (Low→Medium, Medium→High, High→Critical). Notifies Fleet Manager. (L2-034)
- **Service Reminder Workflow**: Runs daily. Queries equipment with scheduled service within 7/3/1 days. Sends reminder notifications to assigned Fleet Manager. (L2-034)

### 3.4 Angular Service Module
- **WorkOrderListComponent**: Kendo Grid with status filter tabs (All, Open, In Progress, etc.)
- **WorkOrderDetailComponent**: Status badge, action buttons (context-sensitive), history timeline
- **ServiceCalendarComponent**: Kendo Scheduler with day/week/month views, drag-and-drop rescheduling
- **CreateWorkOrderDialogComponent**: Modal form with equipment autocomplete search

## 4. Data Model

### 4.1 Class Diagram
![Class Diagram](diagrams/class_diagram.png)

### 4.2 Key Indexes
- `IX_WorkOrders_OrganizationId_Status` — status tab queries
- `IX_WorkOrders_EquipmentId` — equipment detail service history
- `IX_WorkOrders_AssignedToUserId_Status` — technician's active WOs
- `IX_WorkOrders_ScheduledDate` — calendar range queries

## 5. Key Workflows

### 5.1 Work Order Lifecycle
![Sequence Diagram](diagrams/sequence_wo_lifecycle.png)

## 6. API Contracts

### POST /api/v1/work-orders
```json
// Request
{
  "equipmentId": "guid",
  "serviceType": "Corrective",
  "priority": "High",
  "description": "Hydraulic leak on boom cylinder",
  "requestedDate": "2026-04-05",
  "assignedToUserId": "guid"
}
// Response 201
{
  "id": "guid",
  "workOrderNumber": "WO-20260401-001",
  "status": "Open",
  ...
}
```

### PUT /api/v1/work-orders/{id}/status
```json
// Request
{ "status": "InProgress", "notes": "Starting repair" }
// Response 200
{ "id": "guid", "status": "InProgress", "updatedAt": "..." }
```

## 7. Security Considerations
- Status transitions validated server-side — UI button visibility is convenience, not enforcement
- Only the assigned technician or a Fleet Manager/Admin can transition a specific WO
- Closed WOs are immutable — no further modifications allowed

## 8. Open Questions
1. Should WO numbers reset annually (WO-2026-001) or be globally sequential?
2. Can a work order be reopened after being Closed, or is a new WO required?
