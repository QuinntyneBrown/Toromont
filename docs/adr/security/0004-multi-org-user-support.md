# ADR-0004: Multi-Organization User Support

**Date:** 2026-04-01
**Category:** security
**Status:** Accepted
**Deciders:** Toromont Fleet Hub Architecture Team

## Context

Toromont Fleet Hub operates as a multi-tenant system where each customer organization has isolated data. Design 01 Section 8 Decision 1 resolves the open question about whether users should be allowed to belong to multiple organizations: "Yes, allow it. Add a UserOrganization join table." This requirement arises because consultants and regional managers frequently need to access equipment, work orders, and reports across multiple customer organizations. Without multi-org support, these users would need separate accounts per organization, leading to credential sprawl and a poor user experience. The existing multi-tenant data isolation (ADR security/0003) uses EF Core global query filters scoped to an active OrganizationId, which must continue to function correctly with multi-org users.

## Decision

Support users belonging to multiple organizations via a UserOrganization join table, with per-organization role assignments and an organization switcher in the UI.

## Options Considered

### Option 1: UserOrganization Join Table with Per-Org Roles (Chosen)
- **Pros:** Enables consultants and regional managers to access multiple organizations with a single account; per-organization role assignment allows different permissions in different orgs (e.g., Admin in one, Viewer in another); organization switcher in the UI provides clear context about which organization is active; compatible with existing EF Core global query filters by setting the active OrganizationId per request; default organization on login provides a seamless experience for single-org users.
- **Cons:** Adds complexity to the authorization model with per-org roles; requires UI for organization switching; every request must carry the active organization context; developers must ensure all queries respect the active organization to prevent cross-tenant data leakage.

### Option 2: Single Organization per User
- **Pros:** Simplest authorization model with one user-one org mapping; no ambiguity about which organization's data is being accessed; no risk of accidental cross-organization data exposure.
- **Cons:** Consultants and regional managers would need separate user accounts per organization, leading to credential sprawl; poor user experience switching between accounts; duplicated user profile data across organizations; does not reflect the real-world organizational structure where some users naturally span multiple organizations.

### Option 3: Separate User Accounts per Organization
- **Pros:** Complete data isolation per account; no join table or organization switching complexity; each account has a simple, single role.
- **Cons:** Users must manage multiple sets of credentials or rely on SSO to reduce friction; no unified view across organizations; duplicated audit trails across accounts; administrative overhead for provisioning and deprovisioning multiple accounts for the same person.

### Option 4: Organization Switching via Re-Authentication
- **Pros:** Strong security guarantee that the user explicitly authenticates into each organization context; clear audit trail of organization access.
- **Cons:** Extremely poor user experience requiring re-login to switch organizations; impractical for users who need to frequently compare data across organizations; adds authentication latency to every context switch; SSO would partially mitigate but still involves redirects.

## Consequences

### Positive
- Consultants and regional managers can access all their organizations from a single account, eliminating credential sprawl and improving productivity.
- Per-organization roles provide fine-grained access control, ensuring users only have the permissions appropriate for each organization.
- The organization switcher provides clear visual context about which organization is active, reducing the risk of accidental cross-org actions.
- Existing EF Core global query filters continue to enforce data isolation using the active OrganizationId, requiring minimal changes to the data access layer.
- Default organization assignment ensures single-org users experience no additional complexity.

### Negative
- The authorization model is more complex, with roles resolved per organization rather than globally.
- Every API request must include or resolve the active organization context, adding a small overhead to request processing.
- UI must accommodate the organization switcher, adding a component to the header and state management for the active organization.
- Testing must cover multi-org scenarios including role differences across organizations and organization switching.

### Risks
- Cross-tenant data leakage if a developer writes a query that bypasses the global query filter or ignores the active OrganizationId. Mitigation: EF Core global query filters are applied automatically; code reviews and integration tests verify tenant isolation; static analysis rules flag direct DbSet access that might bypass filters.
- Users may forget which organization is active and perform actions in the wrong context. Mitigation: the organization switcher is prominently displayed in the UI header with the active organization name clearly visible; confirmation dialogs for destructive actions include the organization name.
- Token or session manipulation could allow a user to set an OrganizationId they are not a member of. Mitigation: TenantContextMiddleware validates that the user has an active UserOrganization record for the requested OrganizationId before setting the tenant context.

## Implementation Notes
- **UserOrganization join table** schema: UserId (FK), OrganizationId (FK), Role (enum: Admin, Manager, Technician, Viewer), IsDefault (boolean). Composite primary key on (UserId, OrganizationId).
- **Organization switcher** dropdown in the UI header displays all organizations the user belongs to, with the active organization highlighted.
- **Active organization** is sent via the `X-Organization-Id` HTTP header on each API request, or stored in the user's session/claims.
- **TenantContextMiddleware** reads the active organization from the `X-Organization-Id` header, validates the user's membership in that organization via the UserOrganization table, and sets the tenant context for EF Core global query filters.
- **Default organization** (IsDefault = true) is loaded on login when no explicit organization is specified.
- **JWT claims** include the list of OrganizationIds the user belongs to, enabling client-side organization switching without an additional API call.
- **EF Core global query filters** continue to use `_tenantContext.OrganizationId` for all multi-tenant entities, unchanged from the current implementation.

## References
- [L1-007: User Management Design](../../design/L1-007-user-management.md)
- [L1-010: Multi-Tenancy Design](../../design/L1-010-multi-tenancy.md)
- [ADR security/0003: Multi-Tenant Data Isolation](0003-multi-tenant-data-isolation.md)
- [ADR security/0002: JWT Role-Based Access Control](0002-jwt-role-based-access-control.md)
