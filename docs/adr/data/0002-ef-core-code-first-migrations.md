# ADR-0002: EF Core Code-First Approach with Migrations for Database Schema Management

**Date:** 2026-04-01
**Category:** data
**Status:** Accepted
**Deciders:** Toromont Fleet Hub Architecture Team

## Context

The Toromont Fleet Hub requires a repeatable, version-controlled approach to database schema management that aligns with domain model evolution. The system defines 13+ entity types (Equipment, MaintenanceRecord, TelemetryEvent, Alert, PartsOrder, User, etc.) with complex relationships and constraints. Schema changes must propagate reliably across local development, staging, and production environments as part of the CI/CD pipeline. The team works primarily in C# and needs a schema management strategy that fits naturally into the .NET development workflow.

## Decision

Use EF Core code-first approach with migrations as the sole mechanism for database schema management. The FleetHubDbContext defines all entity configurations using Fluent API, and schema changes are captured as versioned migration files committed alongside application code.

## Options Considered

### Option 1: EF Core Code-First with Migrations (chosen)

- **Pros:**
  - Schema is defined in C# code directly alongside domain models, providing a single source of truth
  - Automatic migration generation from model changes via `dotnet ef migrations add` reduces manual SQL authoring
  - Migration files are version-controlled in Git, enabling full audit trail of schema evolution
  - Supports rollback with `dotnet ef database update <previous-migration>` for safe deployments
  - Integrates with CI/CD pipeline; migrations can be applied automatically during deployment or via a dedicated migration step
  - Fluent API configuration in OnModelCreating provides fine-grained control over table mappings, indexes, relationships, and constraints
  - Foreign keys and relationships are configured explicitly, making the data model self-documenting
  - EF Core tooling validates model consistency at migration generation time, catching errors early
- **Cons:**
  - Complex migrations (data transformations, conditional logic) can be difficult to express in the migration framework
  - Generated SQL may not always be optimal; manual review of migration SQL is required for production deployments
  - Large teams working on concurrent feature branches may encounter migration merge conflicts
  - EF Core migration tooling has a learning curve for developers unfamiliar with the framework

### Option 2: Database-First with Scaffolding

- **Pros:**
  - DBAs can design the schema using familiar tools (SSMS, Azure Data Studio)
  - Full control over physical database design from the start
  - Scaffolding generates entity classes automatically
- **Cons:**
  - Schema and code can drift apart; scaffolding must be re-run after every database change
  - No version-controlled migration history by default
  - Breaks the domain-driven design workflow where models drive the schema
  - Harder to automate in CI/CD pipelines

### Option 3: SQL Migration Scripts (DbUp / Flyway)

- **Pros:**
  - Full control over every SQL statement executed against the database
  - Framework-agnostic; works with any database and any application stack
  - Mature tooling with proven track record in large enterprises
- **Cons:**
  - Requires manual authoring of every schema change as raw SQL
  - No automatic detection of model changes; developers must manually keep SQL scripts in sync with C# models
  - Dual maintenance burden: C# entity classes and SQL migration scripts must evolve in lockstep
  - Rollback scripts must be authored manually for each migration

### Option 4: Manual Schema Management

- **Pros:**
  - No tooling overhead or framework dependency
  - Full flexibility for one-off changes
- **Cons:**
  - No repeatability or version control of schema changes
  - High risk of environment drift between development, staging, and production
  - No audit trail for schema evolution
  - Completely unsuitable for team-based development and automated deployments

## Consequences

### Positive

- The FleetHubDbContext with 13 DbSets serves as the definitive schema definition, reducing ambiguity about the current database structure
- Fluent API configuration in OnModelCreating ensures indexes, foreign keys, constraints, and relationships are explicitly declared and reviewable in code review
- Migration files in the `Migrations/` folder provide a chronological history of every schema change
- CI/CD pipeline can apply pending migrations automatically, ensuring environment consistency
- Developers can generate and test migrations locally before committing, reducing production deployment risk

### Negative

- Team must enforce discipline around migration generation: one migration per logical change, meaningful migration names, and review of generated SQL before merging
- Concurrent development on separate branches may produce conflicting migration snapshots that require manual resolution
- Some advanced SQL Server features (table partitioning, columnstore indexes) may require raw SQL within migrations rather than pure Fluent API

### Risks

- A botched migration applied to production without a tested rollback path could cause downtime; mitigated by always generating and testing rollback scripts in staging first
- EF Core model snapshot divergence after merge conflicts can produce incorrect migrations if not carefully resolved
- Performance of `context.Database.Migrate()` at application startup may not be suitable for production; a separate migration deployment step is recommended

## Implementation Notes

- FleetHubDbContext defines 13 DbSets including Equipment, MaintenanceRecord, TelemetryEvent, Alert, PartsOrder, OrderLineItem, User, Notification, NotificationPreference, AIPrediction, AnomalyDetection, and related entities
- Fluent API configuration in OnModelCreating handles:
  - Composite and unique indexes (e.g., IX_Equipment_OrganizationId_SerialNumber)
  - Foreign key relationships with explicit cascade/restrict delete behavior
  - Property constraints (max length, required, precision for decimal fields)
  - Value conversions for enums stored as strings
- Migration workflow:
  1. Modify entity classes or Fluent API configuration
  2. Run `dotnet ef migrations add <MigrationName>` to generate migration
  3. Review generated migration code and SQL output (`dotnet ef migrations script`)
  4. Commit migration files alongside model changes
  5. CI/CD pipeline applies migrations to target environment
- For production deployments, use `dotnet ef migrations script --idempotent` to generate idempotent SQL scripts that can be reviewed and applied by the operations team

## References

- Traces to: L1-012
- [EF Core Migrations Overview](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [EF Core Fluent API Configuration](https://learn.microsoft.com/en-us/ef/core/modeling/)
- [Managing Migrations in Team Environments](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/teams)
