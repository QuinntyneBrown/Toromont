# ADR-0010: Entity Numbering Schemes

**Date:** 2026-04-01
**Category:** backend
**Status:** Accepted
**Deciders:** Ironvale Fleet Hub Architecture Team

## Context

Design 03 (Work Order Management) specifies work order numbering as WO-YYYYMMDD-NNN where NNN increments per day. Design 04 (Parts & Inventory) specifies parts order numbering as PO-YYYYMMDD-NNN. The system needs human-readable, sortable identifiers that indicate creation date and are easily communicated verbally during phone support, field service, and management discussions. These identifiers supplement the internal database primary keys (GUIDs or auto-increment IDs) and serve as the user-facing reference numbers.

## Decision

Use date-based sequential numbering for work orders (WO-YYYYMMDD-NNN) and parts orders (PO-YYYYMMDD-NNN).

## Options Considered

### Option 1: Date-Based Sequential Numbering (Chosen)
- **Pros:** Human-readable format aids verbal communication and phone support; date component provides instant temporal context (when was this created?); sequential daily counter prevents gaps within a day; prefix (WO/PO) distinguishes entity types at a glance; natural sort order aligns with chronological order; easy to reference in emails, reports, and conversations.
- **Cons:** Requires atomic counter logic to prevent duplicate numbers under concurrent creation; daily counter reset adds implementation complexity; format reveals creation volume (counter portion) which may be undesirable; fixed format may need revision if numbering exceeds 999 per day.

### Option 2: GUID Only
- **Pros:** Globally unique with no coordination needed; no counter management; built-in support in .NET and SQL Server.
- **Cons:** Not human-readable; impossible to communicate verbally; no temporal context; poor user experience when referencing orders in conversation or on paper.

### Option 3: Auto-Increment Integer
- **Pros:** Simple implementation; guaranteed unique; compact format.
- **Cons:** No temporal context; sequential integers reveal total volume; no entity type differentiation without prefix; single global sequence creates a database bottleneck.

### Option 4: UUID with Prefix
- **Pros:** Unique with type prefix for entity differentiation; no counter coordination needed.
- **Cons:** Too long for verbal communication; no temporal context; poor user experience.

### Option 5: Sequential Global Counter with Prefix
- **Pros:** Simple incrementing number with entity prefix (WO-00001); compact and readable.
- **Cons:** No temporal context; single global counter creates contention; counter never resets, eventually producing long numbers.

## Consequences

### Positive
- Users can instantly identify when a work order or parts order was created by reading the number.
- Entity type is immediately clear from the WO/PO prefix, reducing confusion in multi-entity contexts.
- Natural chronological sort order when sorting by the identifier string.
- Phone support and field technicians can easily communicate order numbers verbally.
- Format is concise enough for printed labels, reports, and email subject lines.

### Negative
- Concurrent work order or parts order creation on the same day requires atomic counter management to avoid duplicates.
- Daily counter reset logic adds complexity to the generation code.
- The NNN portion limits to 999 entities per type per day; exceeding this requires format revision.

### Risks
- Race condition during concurrent creation could produce duplicate numbers. Mitigation: use database-level atomic increment (e.g., `SELECT MAX + 1` within a transaction or a dedicated sequence table with row-level locking).
- If daily creation volume exceeds 999 for either entity type, the NNN format overflows. Mitigation: monitor creation volumes; the current fleet size makes this extremely unlikely; format can be extended to NNNN if needed.
- Time zone differences could cause confusion about which "day" a number belongs to. Mitigation: use UTC for all number generation; document this convention.

## Implementation Notes

- **Work orders:** `WO-YYYYMMDD-NNN` where NNN resets to 001 each day (UTC).
- **Parts orders:** `PO-YYYYMMDD-NNN` where NNN resets to 001 each day (UTC).
- Generation logic resides in the respective MediatR command handlers (`CreateWorkOrderHandler`, `SubmitOrderHandler`).
- Unique constraint at the database level on the generated number column to prevent duplicates.
- Consider a dedicated sequence table per entity type:
  ```sql
  CREATE TABLE WorkOrderSequence (
      SequenceDate DATE PRIMARY KEY,
      LastNumber INT NOT NULL DEFAULT 0
  );
  ```
- Atomic increment within a transaction to ensure thread safety under concurrent requests.
- **Open question from designs:** Whether NNN resets daily (as currently specified) or uses a global sequence that never resets. The daily reset is adopted as the baseline; this can be revisited if operational needs change.

## References

- [L1-003: Work Order Management Requirements](../design/L1-003-work-order-management.md)
- [L1-004: Parts & Inventory Management Requirements](../design/L1-004-parts-inventory.md)
- [ADR-0008: MediatR CQRS Pattern](0008-mediatr-cqrs-pattern.md)
