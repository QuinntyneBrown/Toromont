# ADR-0006: Leaflet.js with OpenStreetMap Mapping

**Date:** 2026-04-01
**Category:** frontend
**Status:** Accepted
**Deciders:** Toromont Fleet Hub Architecture Team

## Context

Design 02 (Equipment Management) requires equipment location display on maps, showing where each piece of equipment is currently located. Design 05 (Telemetry & Monitoring) requires GPS trail maps showing equipment movement over time. These mapping needs are documented in Design 02 Section 8 Decision 2 and Design 05 Section 8 Decision 3. The application needs a mapping solution that supports location pins, movement trails, and responsive display across device sizes without incurring per-request API costs.

## Decision

Use Leaflet.js with OpenStreetMap tiles for GPS and equipment location mapping.

## Options Considered

### Option 1: Leaflet.js with OpenStreetMap (Chosen)
- **Pros:** Free and open-source with no per-request API costs; OpenStreetMap tiles are free to use; lightweight library (~40KB gzipped); excellent Angular integration via `ngx-leaflet`; sufficient feature set for equipment location pins and GPS trails; no API key management or billing required; large community with extensive plugin ecosystem; well-documented API.
- **Cons:** OpenStreetMap tiles have less visual polish than commercial alternatives; no built-in street view or satellite imagery (requires third-party tile providers); usage policy requires attribution; high-volume tile requests may require self-hosting a tile server.

### Option 2: Google Maps API
- **Pros:** Industry-leading map quality and satellite imagery; extensive API features including Street View, geocoding, and routing; excellent documentation and support.
- **Cons:** Per-request pricing that scales with usage; requires API key management and billing account; vendor lock-in to Google Cloud Platform; pricing can become significant for applications with many map loads; $200/month free tier may be insufficient for fleet-wide usage.

### Option 3: Mapbox
- **Pros:** High-quality vector maps with customizable styling; good developer experience; generous free tier (50,000 map loads/month); modern API design.
- **Cons:** Per-request pricing beyond free tier; requires API key management; vendor lock-in; customization options exceed what is needed for equipment location display.

### Option 4: Kendo UI Map Component
- **Pros:** Consistent with other Kendo UI components used in the application (see ADR-F0003); integrated theming.
- **Cons:** Less feature-rich than dedicated mapping libraries; limited community and plugin ecosystem for mapping-specific needs; may not support GPS trail rendering natively; ties mapping to the Kendo UI license.

### Option 5: Bing Maps
- **Pros:** Good integration with Microsoft ecosystem; reasonable pricing.
- **Cons:** Per-request pricing; smaller community and plugin ecosystem than Google Maps or Leaflet; less Angular-specific tooling available.

## Consequences

### Positive
- Zero ongoing cost for map tiles and API usage, eliminating a variable cost component from the application.
- No API key management, billing account setup, or usage monitoring required for mapping.
- Lightweight library minimizes impact on frontend bundle size.
- `ngx-leaflet` provides idiomatic Angular integration with directive-based map configuration.
- Leaflet's plugin ecosystem supports additional needs if they arise (clustering, heatmaps, etc.).
- OpenStreetMap data is community-maintained and frequently updated.

### Negative
- OpenStreetMap tile quality and detail may be lower than Google Maps in some regions.
- No built-in satellite or aerial imagery; if needed, requires integrating a third-party tile provider.
- OpenStreetMap usage policy requires visible attribution on the map, consuming minor UI space.
- Self-hosted tile server may be needed if the application generates high tile request volumes.

### Risks
- OpenStreetMap tile servers may experience availability issues during high-traffic periods. Mitigation: configure fallback tile providers; consider caching tiles or using a CDN-backed tile provider like Stamen or CartoDB.
- `ngx-leaflet` library maintenance may lag behind Angular major version updates. Mitigation: the library is actively maintained; Leaflet's vanilla JS API can be used directly as a fallback.
- Map rendering performance may degrade with a large number of equipment markers (100+). Mitigation: use marker clustering via `leaflet.markercluster` plugin.

## Implementation Notes

- **Equipment detail view:** Displays a location pin on the map showing the equipment's current GPS coordinates.
- **Telemetry dashboard:** Displays a GPS trail (polyline) for selected equipment over a user-specified time range.
- Install via `npm install leaflet @asymmetrik/ngx-leaflet`.
- Import `LeafletModule` in the relevant Angular standalone components.
- Responsive map sizing:
  - XS/S breakpoints: fixed 250px height
  - M+ breakpoints: flexible height that fills available space
- OpenStreetMap tile layer configuration:
  ```typescript
  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution: '© OpenStreetMap contributors'
  });
  ```
- Equipment markers use custom icons to distinguish equipment types or statuses.
- GPS trail rendering uses Leaflet's `L.polyline` with color-coded segments if speed or status data is available.

## References

- [L1-002: Equipment Management Requirements](../design/L1-002-equipment-management.md)
- [L1-005: Telemetry & Monitoring Requirements](../design/L1-005-telemetry-monitoring.md)
- [Leaflet Documentation](https://leafletjs.com/)
- [ngx-leaflet GitHub Repository](https://github.com/bluehalo/ngx-leaflet)
- [OpenStreetMap Tile Usage Policy](https://operations.osmfoundation.org/policies/tiles/)
