# ADR-0002: Karma and Jasmine for Frontend Unit Testing

**Date:** 2026-04-01
**Category:** testing
**Status:** Accepted
**Deciders:** Ironvale Fleet Hub Architecture Team

## Context

The Ironvale Fleet Hub Angular frontend needs a unit testing framework for testing components, services, pipes, and other Angular constructs in isolation. The chosen framework must integrate seamlessly with the Angular CLI build pipeline, support real browser testing for accurate DOM behavior, and provide code coverage reporting for quality gates in the CI pipeline.

## Decision

Use the Karma test runner with the Jasmine testing framework for Angular frontend unit testing, following the Angular CLI default testing configuration.

## Options Considered

### Option 1: Karma + Jasmine (chosen)

- **Pros:**
  - Angular CLI default testing setup, requiring zero additional configuration to get started
  - Jasmine's BDD-style syntax (`describe`, `it`, `expect`) provides readable and well-structured test code
  - Karma executes tests in a real browser (Chrome/ChromeHeadless), ensuring accurate DOM and browser API behavior
  - Built-in code coverage reporting via karma-coverage with Istanbul instrumentation
  - Mature ecosystem with extensive Angular-specific documentation and examples
  - Angular TestBed and testing utilities are designed and documented with Jasmine in mind

- **Cons:**
  - Slower test execution compared to JSDOM-based runners due to real browser overhead
  - Karma's architecture is more complex (server + browser launcher) compared to simpler runners
  - Karma project is in maintenance mode, though it remains the Angular CLI default

### Option 2: Jest

- **Pros:**
  - Faster test execution using JSDOM (no real browser needed)
  - Snapshot testing for component template verification
  - Large community and ecosystem beyond Angular

- **Cons:**
  - Not the Angular CLI default; requires additional configuration and third-party packages (jest-preset-angular)
  - JSDOM does not perfectly replicate real browser behavior, leading to subtle discrepancies
  - Angular schematics and documentation assume Jasmine/Karma
  - Migration effort for existing Angular CLI-generated test scaffolding

### Option 3: Vitest

- **Pros:**
  - Extremely fast execution leveraging Vite's native ES module handling
  - Modern API with excellent TypeScript support
  - Growing community momentum

- **Cons:**
  - No official Angular integration; requires significant custom configuration
  - Angular CLI does not support Vitest as a test runner
  - Immature Angular ecosystem support compared to Jasmine and Jest
  - Risk of compatibility issues with Angular's compilation pipeline

### Option 4: Web Test Runner

- **Pros:**
  - Modern, standards-based test runner using real browsers
  - Supports ES modules natively
  - Lightweight architecture

- **Cons:**
  - No Angular CLI integration
  - Smaller community and fewer Angular-specific resources
  - Would require custom setup for Angular TestBed and dependency injection
  - Less mature tooling for code coverage reporting

## Consequences

### Positive

- Zero-configuration setup accelerates developer onboarding; `ng test` works immediately after project scaffolding
- Real browser testing via Karma ensures unit tests accurately reflect runtime behavior
- BDD-style Jasmine syntax produces self-documenting test code that serves as living documentation
- Built-in code coverage reporting supports quality gates without additional tooling
- Consistency with Angular CLI conventions means generated components include `.spec.ts` files with correct imports

### Negative

- Real browser execution makes unit tests slower than JSDOM-based alternatives, particularly for large test suites
- Karma's maintenance-mode status means new features and performance improvements are unlikely
- If Angular CLI migrates away from Karma in a future version, a migration effort will be required

### Risks

- Karma's maintenance-mode status could lead to compatibility issues with future browser versions. Mitigated by Angular CLI's commitment to ensuring their default test runner works, and by the option to migrate to Jest or another runner if needed.
- Test suite execution time may grow as the codebase scales. Mitigated by running tests in ChromeHeadless in CI and using `--watch=false` for single-run execution.

## Implementation Notes

- Unit test files follow the `**/*.spec.ts` naming pattern and are co-located with the source files they test.
- Test configuration is defined in `tsconfig.spec.json`, which extends the base TypeScript configuration with test-specific settings.
- Run tests locally with `ng test` (watch mode) or `ng test --watch=false --browsers=ChromeHeadless` for CI.
- Code coverage is generated with `ng test --code-coverage` and output to the `/coverage/` directory.
- Use Angular TestBed for component and service testing, with `HttpClientTestingModule` for HTTP service isolation.

## References

- L1-012: Testing Strategy
- Angular testing guide: https://angular.io/guide/testing
- Karma documentation: https://karma-runner.github.io/
- Jasmine documentation: https://jasmine.github.io/
