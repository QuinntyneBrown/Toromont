# ADR-0002: JWT Claims-Based Role-Based Access Control (RBAC)

**Date:** 2026-04-01
**Category:** security
**Status:** Accepted
**Deciders:** Ironvale Fleet Hub Architecture Team

## Context

Ironvale Fleet Hub serves multiple user types with distinct responsibilities and access needs within the fleet management domain. Administrators manage users and organizational settings. Fleet Managers oversee equipment inventories, work orders, and reporting. Technicians perform service tasks and order parts. Operators view dashboards and monitor equipment status. The authorization model must enforce these boundaries reliably, integrate cleanly with the JWT-based authentication established in ADR-0001, and avoid unnecessary complexity given the current set of well-defined roles.

## Decision

Implement Role-Based Access Control (RBAC) using JWT claims issued by Microsoft Entra ID. Define four roles -- Admin, FleetManager, Technician, and Operator -- as Entra ID app roles mapped to the `roles` claim in the access token. On the backend, use ASP.NET Core's built-in `[Authorize(Roles = "...")]` attribute to enforce role requirements at the controller and action level. On the frontend, read role claims from the decoded token to conditionally render UI elements and guard routes. Enforce role-based rules on work order status transitions (e.g., only Technician or FleetManager can transition a work order to InProgress).

## Options Considered

### Option 1: JWT Claims-Based RBAC with Four Roles (chosen)

- **Pros:**
  - Simple role model maps directly to Ironvale's organizational structure and job functions
  - Role information travels inside the JWT, so authorization decisions require no additional database lookups at request time
  - ASP.NET Core provides first-class support via `[Authorize(Roles = "...")]` and policy-based authorization middleware
  - Entra ID app roles are manageable through the Azure portal and assignable to users and groups
  - Four roles provide sufficient granularity for current requirements without over-engineering

- **Cons:**
  - Adding new roles or fine-grained permissions in the future requires changes to Entra ID app registration, backend policies, and frontend guards
  - Role changes for a user do not take effect until the next token issuance (up to 30 minutes with current token lifetime)
  - Cannot express conditional or context-dependent access rules (e.g., "FleetManager but only for Region X") without extending beyond pure RBAC

### Option 2: Attribute-Based Access Control (ABAC)

- **Pros:**
  - Highly flexible; can express fine-grained policies based on user attributes, resource attributes, and environmental conditions
  - Supports context-dependent rules such as geographic or time-based restrictions

- **Cons:**
  - Significantly more complex to implement, test, and reason about
  - Requires a policy evaluation engine (e.g., OPA, custom middleware) adding operational overhead
  - Over-engineered for the current four-role model with well-defined boundaries

### Option 3: Permission-Based Access Control Lists (ACL)

- **Pros:**
  - Allows per-resource, per-user permission assignments for maximum granularity
  - Familiar model for file-system-like access patterns

- **Cons:**
  - Requires a permission management subsystem and database tables for user-resource-permission mappings
  - Administrative overhead grows with the number of resources and users
  - Not well-suited to the role-oriented organizational model at Ironvale

### Option 4: Policy-Based Authorization (standalone)

- **Pros:**
  - Decouples authorization logic from application code into external policy definitions
  - Supports complex, composable rules

- **Cons:**
  - Introduces additional infrastructure (policy engine, policy store)
  - Complexity is disproportionate to the current authorization requirements
  - Steeper learning curve for the development team

## Consequences

### Positive

- Authorization checks are declarative and visible in the codebase via `[Authorize(Roles = "...")]` attributes, making access control easy to audit
- No additional database queries are needed for role-based authorization since roles are embedded in the JWT
- Role assignments are centrally managed in Entra ID, benefiting from existing group-based assignment workflows
- Frontend can use the same role claims to tailor the UI, hiding or disabling features that a user's role does not permit
- Work order status transition rules are explicit and enforceable at the API layer, preventing unauthorized state changes

### Negative

- The four-role model is relatively coarse; if future requirements demand per-field or per-entity permissions, the model will need to evolve toward ABAC or hybrid approaches
- A user's effective permissions can be stale for up to the token lifetime window after a role change in Entra ID
- Dual enforcement (frontend UI guards and backend attribute checks) must be kept in sync to avoid confusing user experiences

### Risks

- Role creep: as the application evolves, pressure to add sub-roles or exceptions may erode the simplicity of the four-role model
- If role claims are not properly validated on the backend (e.g., trusting frontend-only checks), privilege escalation vulnerabilities could arise
- Work order status transition rules embedded in application code must be thoroughly tested to prevent bypasses

## Implementation Notes

- Define the four app roles (Admin, FleetManager, Technician, Operator) in the Entra ID app registration manifest
- Map Entra ID security groups to app roles for streamlined user provisioning
- Role capabilities:
  - **Admin:** Full system access including user management, organization configuration, audit log review, and all operational functions
  - **FleetManager:** Equipment management, work order creation and oversight, parts inventory, report generation, and alert configuration
  - **Technician:** View and update assigned work orders, log service activities, order parts, and view equipment details
  - **Operator:** View dashboards, monitor equipment status, view alerts, and submit basic service requests
- Work order status transition rules enforced at the API layer:
  - Only Technician or FleetManager can move a work order to InProgress
  - Only Technician can move a work order to Completed
  - Only FleetManager can close a work order (Completed → Closed)
- Use ASP.NET Core authorization policies for transitions that involve more than a simple role check
- Frontend Angular route guards check role claims and redirect unauthorized users to an appropriate landing page
- Log all authorization failures for security monitoring and audit purposes

## References

- L1-010: Authentication & Authorization
- L1-007: Core Equipment & Fleet Management
- ADR-0001: Microsoft Entra ID Authentication with OAuth2 Authorization Code Flow + PKCE
- [ASP.NET Core Role-Based Authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles)
- [Microsoft Entra ID App Roles](https://learn.microsoft.com/en-us/entra/identity-platform/howto-add-app-roles-in-apps)
