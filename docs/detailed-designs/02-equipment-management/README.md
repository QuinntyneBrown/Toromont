# Equipment Management — Detailed Design

## 1. Overview

This feature provides the core equipment registry for Toromont Fleet Hub. Users can register, view, edit, and decommission heavy equipment assets. The system maintains complete records including specifications, GPS location, service history references, and telemetry summaries. Legacy XML data imports from existing systems are supported.

**Traces to:** L1-002, L1-011 | **L2:** L2-004, L2-005, L2-006, L2-026

**Actors:** Admin (full CRUD), Fleet Manager (full CRUD), Operator (read-only), Technician (read-only)

## 2. Architecture

### 2.1 C4 Component Diagram
![C4 Component](diagrams/c4_component.png)

## 3. Component Details

### 3.1 Equipment Controller (`EquipmentController`)
- **Responsibility**: RESTful CRUD endpoints for equipment management
- **Endpoints**:
  - `GET /api/v1/equipment` — paginated list with filters (status, category, location, search)
  - `GET /api/v1/equipment/{id}` — detail with specs, latest telemetry, recent service history
  - `POST /api/v1/equipment` — register new equipment (Admin, Fleet Manager)
  - `PUT /api/v1/equipment/{id}` — update equipment (Admin, Fleet Manager)
  - `DELETE /api/v1/equipment/{id}` — decommission (Admin only, soft delete)
- **Pagination**: `?page=1&pageSize=20&sortBy=name&sortDir=asc`
- **Filters**: `?status=Operational&category=Excavator&search=CAT+320`

### 3.2 Import Controller (`ImportController`)
- **Responsibility**: Handles legacy XML equipment data imports
- **Endpoint**: `POST /api/v1/equipment/import` — multipart file upload (max 10MB)
- **Authorization**: Admin and Fleet Manager only
- **Process**: Validates XML schema → parses records → upserts by serial number → returns summary

### 3.3 Equipment Service (MediatR Handlers)
- **CreateEquipmentHandler**: Validates serial number uniqueness within org, creates record
- **UpdateEquipmentHandler**: Validates ownership, applies updates, records audit trail
- **GetEquipmentListHandler**: Builds query with filters, pagination, sorting via EF Core
- **GetEquipmentDetailHandler**: Joins equipment with latest telemetry and 5 most recent work orders

### 3.4 Import Service (`EquipmentImportService`)
- **Responsibility**: Parses XML files, validates against XSD schema, transforms to Equipment entities
- **Duplicate Handling**: Matches on `SerialNumber` within organization — updates existing, creates new
- **Error Handling**: Skips invalid records, continues processing, returns detailed error report

### 3.5 Angular Equipment Module
- **EquipmentListComponent**: Kendo UI Grid with server-side pagination, sorting, filtering
- **EquipmentDetailComponent**: Tabbed view with specs, map, telemetry cards, service timeline
- **EquipmentFormComponent**: Reactive form with validation for add/edit
- **Responsive**: Grid → card layout below 768px (L2-032)

## 4. Data Model

### 4.1 Class Diagram
![Class Diagram](diagrams/class_diagram.png)

### 4.2 Key Database Indexes
- `IX_Equipment_OrganizationId_SerialNumber` (unique) — prevent duplicates per org
- `IX_Equipment_OrganizationId_Status` — fast status filtering
- `IX_Equipment_OrganizationId_Category` — fast category filtering

## 5. Key Workflows

### 5.1 Register New Equipment
![Sequence Diagram](diagrams/sequence_register.png)

### 5.2 Legacy XML Import
1. Admin uploads XML file via `POST /api/v1/equipment/import`
2. Server validates file size (≤10MB) and content type
3. `EquipmentImportService` validates XML against XSD schema
4. Each record is parsed: serial number matched against existing equipment in org
5. Existing records updated, new records created, invalid records skipped
6. Import result summary returned with counts and error details

## 6. API Contracts

### GET /api/v1/equipment
```json
// Response 200
{
  "data": [
    {
      "id": "guid",
      "name": "CAT 320 GC Excavator",
      "make": "Caterpillar",
      "model": "320 GC",
      "year": 2024,
      "serialNumber": "ZAP00321",
      "category": "Excavator",
      "status": "Operational",
      "location": "Toronto, ON",
      "latitude": 43.7001,
      "longitude": -79.4163,
      "lastServiceDate": "2026-03-15"
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalCount": 156,
    "totalPages": 8
  }
}
```

### POST /api/v1/equipment
```json
// Request
{
  "name": "CAT 336 Next Gen",
  "make": "Caterpillar",
  "model": "336 NG",
  "year": 2025,
  "serialNumber": "NGX00543",
  "category": "Excavator",
  "status": "Operational",
  "gpsDeviceId": "GPS-4521",
  "purchaseDate": "2025-01-15",
  "warrantyExpiration": "2028-01-15",
  "notes": "Purchased for Hamilton site"
}

// Response 201
{ "id": "guid", ...equipment fields... }
```

## 7. Security Considerations

- Serial number uniqueness enforced at both application and database constraint level
- XML import validates against XSD schema to prevent XXE attacks — `DtdProcessing.Prohibit`
- File upload limited to 10MB with content-type validation
- All queries automatically tenant-filtered via EF Core global query filter

## 8. Open Questions

1. Should equipment decommission be a soft delete (status change) or hard delete with archival?
2. What GPS coordinate precision is required — should we store raw device coordinates or geocoded addresses?
3. Should the legacy XML import support scheduled/automated imports or manual upload only?
