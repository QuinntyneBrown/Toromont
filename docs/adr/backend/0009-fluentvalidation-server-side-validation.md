# ADR-0009: FluentValidation Server-Side Validation

**Date:** 2026-04-01
**Category:** backend
**Status:** Accepted
**Deciders:** Ironvale Fleet Hub Architecture Team

## Context

All 7 detailed designs mandate FluentValidation in their security sections. Every API input must be validated server-side regardless of client-side validation. The application needs consistent, testable validation that integrates with the MediatR CQRS pipeline (see ADR-0008). Client-side validation exists for user experience purposes only; server-side validation is the authoritative enforcement point for data integrity and security.

## Decision

Use FluentValidation for server-side input validation on all API endpoints.

## Options Considered

### Option 1: FluentValidation (Chosen)
- **Pros:** Fluent builder syntax produces readable, self-documenting validation rules; integrates with MediatR pipeline behaviors for automatic validation before handler execution; strongly typed validators per request class; easy to unit test validators in isolation; clean separation of validation logic from business logic; consistent structured error response format; large .NET community with active maintenance.
- **Cons:** Additional library dependency; requires creating a validator class per request; developers must learn the fluent API syntax.

### Option 2: Data Annotations
- **Pros:** Built into .NET with no additional dependencies; familiar to most .NET developers; automatic model binding validation in ASP.NET Core.
- **Cons:** Limited expressiveness for complex validation rules; attribute-based approach clutters model classes; difficult to express cross-property or conditional validation; harder to unit test validation logic independently; does not integrate cleanly with MediatR pipeline.

### Option 3: Manual Validation in Handlers
- **Pros:** No additional library needed; full control over validation logic; validation is co-located with business logic.
- **Cons:** Validation logic mixed with business logic violates single responsibility principle; inconsistent validation patterns across handlers; validation code is duplicated for shared rules; harder to ensure all inputs are validated; no automatic pipeline integration.

### Option 4: Custom Validation Framework
- **Pros:** Tailored exactly to application needs; no external dependency.
- **Cons:** Significant development effort; lacks community support and battle-testing; reinvents well-solved problems; maintenance burden falls entirely on the team.

## Consequences

### Positive
- Every API request is automatically validated before reaching the handler, ensuring no unvalidated input is processed.
- Fluent syntax produces validation rules that read like specifications, improving code reviewability.
- Validators are independently unit testable, enabling comprehensive validation coverage.
- Consistent error response format across all endpoints: 400 Bad Request with structured error details including field names and error messages.
- Separation of validation from business logic keeps handlers focused on their core responsibility.
- MediatR pipeline integration means developers cannot accidentally skip validation.

### Negative
- Each command/query request requires a corresponding validator class, increasing the number of files per feature.
- Developers must learn FluentValidation's API beyond basic rules for advanced scenarios (conditional validation, custom validators, collection validation).
- Validation rules in FluentValidation are separate from the request class, requiring navigation between files to understand the full contract.

### Risks
- Validation rules may diverge from client-side validation, causing confusing user experiences where the client accepts input the server rejects. Mitigation: document validation rules in API specifications that both frontend and backend teams reference.
- Complex validation logic in FluentValidation may become difficult to maintain. Mitigation: decompose complex validators using `Include()` and custom validator classes.
- Performance impact of validation on high-throughput endpoints (e.g., telemetry ingestion). Mitigation: keep validators lightweight; move expensive validation (e.g., database lookups) to the handler.

## Implementation Notes

- One validator class per command/query request (e.g., `CreateEquipmentCommandValidator`, `SubmitOrderCommandValidator`).
- Register as MediatR pipeline behavior via `ValidationBehavior<TRequest, TResponse>` that runs before the handler.
- Validation errors return HTTP 400 Bad Request with a structured response:
  ```json
  {
    "errors": {
      "FieldName": ["Error message 1", "Error message 2"]
    }
  }
  ```
- Client-side validation is for UX only; server-side validation via FluentValidation is authoritative.
- Security-relevant validation includes:
  - Preventing SQL injection via parameterized queries combined with validated inputs
  - Preventing XSS via output encoding combined with validated inputs
  - All telemetry payloads validated before processing to prevent malformed data ingestion
- Register all validators automatically: `builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly)`.

## References

- [L1-013: Security & Compliance Requirements](../design/L1-013-security-compliance.md)
- [ADR-0008: MediatR CQRS Pattern](0008-mediatr-cqrs-pattern.md)
- [FluentValidation Documentation](https://docs.fluentvalidation.net/)
