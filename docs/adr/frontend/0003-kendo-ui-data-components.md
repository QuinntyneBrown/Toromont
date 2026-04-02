# ADR-0003: Kendo UI for Angular Data Components

**Date:** 2026-04-01
**Category:** frontend
**Status:** Accepted
**Deciders:** Ironvale Fleet Hub Architecture Team

## Context

Ironvale Fleet Hub requires rich data visualization and interaction components across several core features: equipment inventory grids with server-side pagination, sorting, and filtering; work order scheduling with day, week, and month calendar views; telemetry data presented as line charts, bar charts, and geographic maps; and KPI dashboards showing fleet utilization, uptime, and cost metrics. These components must handle large datasets efficiently, support accessibility standards, and maintain a consistent visual language across the application.

## Decision

Use Telerik Kendo UI for Angular as the primary component library for data grids, schedulers, and charts. Kendo UI provides a comprehensive suite of native Angular components from a single vendor, ensuring API consistency, unified theming, and enterprise-grade support.

## Options Considered

### Option 1: Kendo UI for Angular (chosen)

- **Pros:**
  - Comprehensive component suite covering Grid, Scheduler, Charts, and 100+ additional components from one vendor
  - Native Angular implementation (not jQuery wrappers), providing proper change detection and lifecycle integration
  - Built-in server-side data binding with support for OData-style queries (pagination, sorting, filtering)
  - Scheduler component supports day, week, and month views with drag-and-drop rescheduling
  - Charts library includes line, bar, area, pie, and map visualizations
  - Enterprise support with SLAs and dedicated technical assistance
  - Built-in accessibility (WCAG 2.1 AA compliance) and keyboard navigation
  - Consistent theming across all components via Kendo theme builder

- **Cons:**
  - Commercial license cost adds to project budget
  - Large package size if all component modules are imported (mitigated by selective imports)
  - Vendor lock-in to Telerik/Progress for data component implementations
  - Learning curve for Kendo-specific APIs and data binding patterns

### Option 2: AG Grid + Chart.js

- **Pros:**
  - AG Grid is the industry leader for high-performance data grids
  - Chart.js is lightweight and easy to configure
  - Both have free community editions

- **Cons:**
  - Two separate libraries with different APIs, theming approaches, and upgrade cycles
  - No scheduler component; would need a third library (e.g., FullCalendar)
  - AG Grid enterprise features (row grouping, server-side row model) require a commercial license
  - Inconsistent visual language across three different component libraries

### Option 3: PrimeNG

- **Pros:**
  - Large component library with Angular-native implementations
  - Open-source core with commercial theme support
  - Active community and frequent updates

- **Cons:**
  - Grid component is less powerful than Kendo Grid or AG Grid for complex server-side scenarios
  - Scheduler component is less feature-rich than Kendo Scheduler
  - Charting capabilities are more limited than dedicated charting libraries
  - Less polished accessibility support compared to Kendo UI

### Option 4: Angular Material + D3.js

- **Pros:**
  - Angular Material provides accessible, well-tested basic components
  - D3.js offers complete control over chart visualizations
  - Both are open-source with strong communities

- **Cons:**
  - Angular Material's `mat-table` lacks built-in server-side pagination, sorting, and filtering
  - No scheduler component in Angular Material
  - D3.js has a steep learning curve and requires significant custom code for each chart type
  - Maintaining custom D3.js visualizations is a long-term burden

## Consequences

### Positive

- Single vendor for all data-intensive components ensures consistent APIs, theming, and documentation
- Server-side data binding integrates cleanly with the ASP.NET Core backend API for paginated equipment lists
- Scheduler component directly addresses the work order calendar requirement with day, week, and month views
- Charts library covers all telemetry visualization needs (line charts for time-series data, bar charts for comparisons, maps for fleet location)
- Enterprise support provides a safety net for complex issues and guaranteed response times
- Accessibility compliance is handled by the component library rather than requiring custom ARIA implementations

### Negative

- Commercial license cost must be budgeted and renewed annually
- Team members must learn Kendo-specific APIs and patterns alongside Angular fundamentals
- Vendor lock-in means migrating away from Kendo UI would require significant effort

### Risks

- If Telerik/Progress changes licensing terms or pricing, the project budget could be impacted
- Over-reliance on Kendo components may limit customization for highly specific UI requirements
- Large Kendo package imports could affect bundle size if tree-shaking is not properly configured

## Implementation Notes

- Import Kendo modules selectively per feature:
  - `@progress/kendo-angular-grid` for equipment inventory and parts order grids
  - `@progress/kendo-angular-scheduler` for work order calendar views
  - `@progress/kendo-angular-charts` for telemetry line/bar charts and fleet KPI dashboards
  - `@progress/kendo-angular-inputs`, `@progress/kendo-angular-dropdowns` for form elements
- Configure Kendo Grid with server-side `DataBindingDirective` for equipment lists (remote pagination, sorting, filtering)
- Scheduler configuration:
  - Day view: hourly slots for detailed work order planning
  - Week view: default view for fleet managers
  - Month view: high-level overview of scheduled maintenance
- Chart configuration:
  - Telemetry line charts: time-series with zoom and pan for engine hours, fuel consumption, temperature
  - Bar charts: equipment utilization comparisons across fleet
  - KPI dashboard: gauge charts for uptime percentage, sparklines for trends
- Apply Kendo theme customization via `@progress/kendo-theme-default` SCSS overrides to match Ironvale branding

## UI Design Mapping

The Kendo UI components are designed in `docs/ui-design.pen` under the **"Kendo UI Components"** frame, which contains reusable component definitions for:

| Kendo Component | .pen Reusable Node | Used In Screens |
|---|---|---|
| `kendo-grid` | Kendo/Grid | 03 - Equipment List, 05 - Work Orders, 06 - Parts Catalog, 08 - AI Insights, 10 - User Management |
| `kendo-dropdownlist` | Kendo/DropDownList | 03 - Equipment List (Status, Category), 06 - Parts Catalog (Compatibility), 07 - Telemetry (Equipment selector), 09 - Reports (Equipment filter) |
| `kendo-tabstrip` | Kendo/TabStrip | 05 - Work Orders (status tabs) |
| `kendo-daterange` | Kendo/DateRangePicker | 09 - Reports (date range) |
| `kendo-chart` (Line) | Kendo/Chart/Line | 07 - Telemetry (Engine Hours, Temperature) |
| `kendo-chart` (Bar) | Kendo/Chart/Bar | 07 - Telemetry (Fuel), 09 - Reports (Utilization) |
| `kendo-chart` (Pie) | Kendo/Chart/Pie | 09 - Reports (Cost Breakdown) |
| `kendo-scheduler` | Kendo/Scheduler | 05 - Work Orders (calendar view) |

## Detailed Design Cross-References

This ADR is implemented across the following detailed designs:

- [01 - Authentication](../../../docs/detailed-designs/01-authentication/README.md) — Kendo Grid for user management table
- [02 - Equipment Management](../../../docs/detailed-designs/02-equipment-management/README.md) — Kendo Grid with server-side pagination, DropDownList filters
- [03 - Service Management](../../../docs/detailed-designs/03-service-management/README.md) — Kendo Grid, TabStrip, **Kendo Scheduler** (day/week/month)
- [04 - Parts & Ordering](../../../docs/detailed-designs/04-parts-ordering/README.md) — Kendo Grid for catalog, DropDownList filters
- [05 - Telemetry & Monitoring](../../../docs/detailed-designs/05-telemetry-monitoring/README.md) — Kendo Charts (Line, Bar), DropDownList, ButtonGroup
- [06 - AI Insights](../../../docs/detailed-designs/06-ai-insights/README.md) — Kendo Grid for predictions
- [07 - Notifications & Reporting](../../../docs/detailed-designs/07-notifications-reporting/README.md) — Kendo Charts (Bar, Pie, Line), DateRangePicker, export controls

## References

- L1-001: Fleet Dashboard requirement
- L1-003: Work Order Management requirement
- L1-005: Telemetry and Monitoring requirement
- L1-009: Reports and Analytics requirement
- UI Design: `docs/ui-design.pen` — Kendo UI Components frame
- Kendo UI for Angular documentation: https://www.telerik.com/kendo-angular-ui
