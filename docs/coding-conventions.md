# Ironvale Coding Conventions

This document defines the coding conventions for the Ironvale solution, with primary emphasis on:

- backend C# code in `src/backend/IronvaleFleetHub.Api`
- frontend Angular code in `src/frontend/src`

## Scope

- These conventions apply to all manually maintained backend code.
- Generated files, migration snapshots, and framework-owned bootstrap artifacts may follow framework conventions when required.
- When a framework requirement conflicts with this document, satisfy the framework requirement and keep the exception narrow and explicit.

## File Organization

- Use one top-level type per `.cs` file.
- Name the file after the type it contains. Examples: `EquipmentController.cs`, `ReportResponse.cs`, `DevAuthHandler.cs`.
- Do not group unrelated request/response models into bundle files such as `ApiContracts.cs`.
- Keep one namespace declaration per file.
- Prefer file-scoped namespaces.
- Keep folder structure aligned to responsibility. Examples: `Controllers/`, `Models/`, `Services/`, `DTOs/`, `Middleware/`, `Authentication/`.
- `Program.cs` is the only bootstrap file allowed to use top-level statements. Supporting types used by application startup must live in their own files.
- Use partial types only when there is a clear framework or tooling requirement.
- Frontend Angular components must be split by file type:
  `component-name.component.ts`, `component-name.component.html`, and `component-name.component.scss`.
- Do not use inline Angular component templates or inline component styles in `@Component` metadata.
- Keep each Angular component, service, guard, interceptor, and model in its own `.ts` file.
- Keep component assets beside the component TypeScript file rather than in shared template/style bundle files.

## Naming

- Use `PascalCase` for classes, records, interfaces, enums, methods, and public properties.
- Prefix interfaces with `I`.
- Use descriptive names that reflect domain intent instead of technical placeholders.
- Name request and response contracts with explicit suffixes such as `Request`, `Response`, `Dto`, or `Summary` when applicable.
- Name async methods with the `Async` suffix unless an ASP.NET Core action signature or interface contract makes that redundant or impossible.
- Use singular names for types and plural names for collections.

## Language and Style

- Keep nullable reference types enabled and model nullability explicitly.
- Prefer small, focused files and focused types over large multi-purpose classes.
- Prefer explicit access modifiers for top-level members.
- Use object and collection initializers when they improve clarity.
- Keep methods short and cohesive.
- Avoid deep nesting; return early when guard clauses improve readability.
- Add comments only for non-obvious intent, invariants, or framework workarounds.
- Do not add comments that restate the code.
- In Angular templates, keep structural markup in the `.html` file and styling in the `.scss` file instead of embedding either in TypeScript.

## Dependency Injection and Architecture

- Use constructor injection or framework-supported injection patterns for dependencies.
- Register concrete implementations against interfaces in `Program.cs`.
- Keep controllers thin. Controllers should coordinate HTTP concerns and delegate business logic to services.
- Keep domain and persistence logic out of controllers.
- Put cross-cutting request pipeline concerns in middleware, filters, or dedicated services.
- For Angular, keep components focused on presentation and user interaction, and keep data access and cross-cutting logic in injected services.

## Frontend Conventions

- Prefer standalone Angular components unless the project introduces a clear module-based requirement.
- Use `templateUrl` and `styleUrl` for Angular components.
- Name shared UI building blocks with the `.component.ts` suffix and keep the sibling `.html` and `.scss` files aligned to the same basename.
- Keep route screens under `features/` and reusable UI under `shared/components/`.
- Keep global theming, resets, and framework overrides in `src/styles.scss`. Component-specific styles belong in component `.scss` files.
- Avoid inline `style=""` attributes in templates when a reusable class or component stylesheet rule is more appropriate.
- Keep TypeScript-only concerns in `.ts` files. Markup belongs in `.html`. Styling belongs in `.scss`.

## API Contracts

- Store API request and response contracts in `DTOs/`.
- Use one file per contract type.
- Keep DTOs transport-focused. Do not put persistence behavior or domain logic in DTO classes.
- Use explicit request models for write operations instead of loosely typed dictionaries or anonymous payload shapes.
- Use explicit response models when the API shape is part of the contract.

## Entity and Data Access Conventions

- Keep EF Core entity types in `Models/`, one file per entity.
- Configure relationships and persistence behavior in the DbContext or dedicated configuration when needed.
- Avoid leaking EF entities directly into external API contracts when the API shape differs from the persistence model.
- Keep seeding logic in dedicated seeders or data setup classes rather than mixing it into controllers.

## Validation

- Use FluentValidation for request validation.
- Validate incoming API contracts before they reach business logic.
- Keep validation rules close to the contract or feature they protect.
- Prefer explicit validation messages over implicit failures.

## Async and Cancellation

- Use async I/O for database, network, and file operations.
- Pass `CancellationToken` through async call chains where supported.
- Do not block on async code with `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()`.

## Logging and Errors

- Use structured logging through `ILogger` and Serilog.
- Log meaningful operational events, failures, and boundary conditions.
- Do not log secrets, access tokens, or sensitive personal data.
- Return appropriate HTTP status codes and problem details for API failures.
- Keep exception handling centralized where possible.

## Testing and Change Discipline

- Preserve buildability after each refactor.
- Refactors that change file layout must not change runtime behavior unless the change is intentional and validated.
- Run the relevant build and tests after structural changes.
- Prefer incremental, reviewable changes over sweeping rewrites.

## Pull Request Expectations

- Keep naming, folder placement, and file-per-type consistency in every change set.
- If a new convention is needed, update this document in the same change that introduces it.
- Avoid introducing new exceptions to these conventions without documenting the reason.
