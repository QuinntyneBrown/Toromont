# ADR-0001: Playwright for End-to-End Testing

**Date:** 2026-04-01
**Category:** testing
**Status:** Accepted
**Deciders:** Toromont Fleet Hub Architecture Team

## Context

The Toromont Fleet Hub Angular frontend requires reliable end-to-end testing to validate critical user flows across the platform. These flows include equipment management (CRUD operations, status tracking), work order lifecycle (creation through completion), parts ordering (search, cart, checkout), and telemetry dashboards (real-time data visualization, chart interactions). The E2E test suite must run across multiple browsers to ensure broad compatibility, support parallel execution for fast CI feedback, and provide strong debugging capabilities to quickly diagnose failures.

## Decision

Use Playwright as the end-to-end testing framework for the Angular frontend, with the test suite located in the `/e2e/` directory.

## Options Considered

### Option 1: Playwright (chosen)

- **Pros:**
  - Cross-browser support out of the box (Chromium, Firefox, WebKit) with a single API
  - Auto-waiting mechanism eliminates the most common source of flaky tests by automatically waiting for elements to be actionable
  - Native parallel test execution across multiple workers for fast CI runs
  - Built-in test fixtures and assertions designed for web testing
  - Excellent TypeScript support, aligning with the Angular frontend language
  - Powerful debugging tools: traces, screenshots, and video recording on failure
  - Network interception and mocking for isolating frontend tests from backend dependencies
  - Active development and strong community momentum

- **Cons:**
  - Newer framework compared to Selenium, with a smaller (but rapidly growing) ecosystem
  - Team may need initial ramp-up time if experienced primarily with other frameworks
  - Browser binaries must be downloaded and managed (handled by Playwright CLI)

### Option 2: Cypress

- **Pros:**
  - Excellent developer experience with interactive test runner
  - Large ecosystem of plugins and community support
  - Built-in dashboard service for test analytics

- **Cons:**
  - Limited cross-browser support (no native WebKit/Safari testing)
  - Runs inside the browser, which imposes architectural limitations (no multi-tab, limited iframe support)
  - Parallel execution requires paid Cypress Cloud or custom orchestration
  - Historically slower test execution compared to Playwright

### Option 3: Selenium

- **Pros:**
  - Longest-established E2E testing framework with extensive documentation
  - Supports every major browser and multiple programming languages
  - Large pool of developers with Selenium experience

- **Cons:**
  - Requires explicit waits and sleep statements, leading to flaky tests
  - Verbose API compared to modern alternatives
  - No built-in parallel execution; requires Selenium Grid setup
  - Slower test execution and higher infrastructure overhead

### Option 4: TestCafe

- **Pros:**
  - No browser plugin or WebDriver dependency
  - Built-in waiting mechanism
  - Simple setup and configuration

- **Cons:**
  - Smaller community and ecosystem compared to Playwright and Cypress
  - TypeScript support is available but not as deeply integrated
  - Fewer debugging and tracing tools
  - Uncertain long-term investment and development velocity

## Consequences

### Positive

- Cross-browser coverage ensures the application works for all users regardless of browser choice
- Auto-waiting significantly reduces flaky tests, improving CI reliability and developer confidence
- Parallel execution keeps the E2E test suite fast as the number of tests grows
- TypeScript-native authoring provides type safety and IDE support consistent with the Angular codebase
- Traces and screenshots accelerate failure diagnosis, reducing time spent debugging CI failures

### Negative

- Team members unfamiliar with Playwright will need onboarding and training
- Browser binary management adds a step to CI pipeline setup and increases CI cache size
- Playwright's rapid release cadence may occasionally require updating test code for breaking changes

### Risks

- Playwright is newer than Selenium and Cypress; if adoption slows, long-term support could be affected. Mitigated by Microsoft's active investment and Playwright's strong adoption trajectory.
- E2E tests are inherently slower and more brittle than unit tests; the team must maintain discipline around test scope to avoid an overly large and slow E2E suite.

## Implementation Notes

- E2E tests are located in the `/e2e/` directory at the project root.
- Configure Playwright to run against Chromium, Firefox, and WebKit in CI, with Chromium as the default for local development.
- Use Playwright's built-in fixtures for authentication state reuse across tests to avoid redundant login flows.
- Capture traces and screenshots on test failure for CI debugging.
- Organize tests by feature area: equipment, work orders, parts ordering, and telemetry dashboards.

## References

- L1-012: Testing Strategy
- Playwright documentation: https://playwright.dev/
