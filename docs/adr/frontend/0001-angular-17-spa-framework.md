# ADR-0001: Angular 17 SPA Framework

**Date:** 2026-04-01
**Category:** frontend
**Status:** Accepted
**Deciders:** Toromont Fleet Hub Architecture Team

## Context

Toromont Fleet Hub requires an enterprise-grade single-page application (SPA) framework to deliver a fleet management dashboard, equipment management views, work order tracking, parts ordering, telemetry visualization, and AI-driven insights. The frontend must support complex forms, data-heavy grids, real-time updates, and a responsive layout across desktop and mobile devices. The chosen framework must provide strong typing, scalable project structure, and long-term maintainability for a multi-year enterprise product.

## Decision

Use Angular 17 with TypeScript 5.4 as the frontend SPA framework. The project targets ES2022, enables TypeScript strict mode, and leverages Angular CLI for scaffolding, building, and testing.

## Options Considered

### Option 1: Angular 17 with TypeScript 5.4 (chosen)

- **Pros:**
  - Strong TypeScript integration with strict mode support ensures type safety across the codebase
  - Opinionated project structure is well-suited to enterprise applications with large teams
  - Built-in routing, reactive forms, and HTTP client reduce reliance on third-party libraries
  - Excellent tooling via Angular CLI for code generation, builds, and testing
  - Long-term support backed by Google with predictable release cadence
  - Native compatibility with Kendo UI for Angular, the selected component library
  - ES2022 target enables modern JavaScript features (top-level await, private fields)

- **Cons:**
  - Steeper learning curve compared to React or Vue for developers new to Angular
  - Larger initial bundle size compared to lighter frameworks
  - More verbose boilerplate, though mitigated by standalone components in Angular 17

### Option 2: React 18

- **Pros:**
  - Largest ecosystem and community; abundant third-party libraries
  - Flexible, component-driven architecture with hooks
  - Strong hiring pool of React developers

- **Cons:**
  - Unopinionated structure requires assembling routing, state management, and form handling from separate libraries
  - TypeScript support is opt-in and less deeply integrated than Angular
  - No native equivalent to Angular's dependency injection or module system for large-scale organization

### Option 3: Vue 3 with Composition API

- **Pros:**
  - Gentle learning curve and approachable API
  - Good TypeScript support via Composition API
  - Smaller bundle size

- **Cons:**
  - Smaller enterprise adoption compared to Angular and React
  - Fewer enterprise-grade component libraries with native Vue support
  - Less opinionated structure can lead to inconsistency in large teams

### Option 4: Next.js (React-based)

- **Pros:**
  - Server-side rendering and static site generation out of the box
  - File-based routing simplifies route management
  - Strong Vercel ecosystem and deployment story

- **Cons:**
  - SSR/SSG capabilities are unnecessary for an authenticated fleet management dashboard
  - Adds server-side complexity that conflicts with the existing ASP.NET Core backend architecture
  - Still inherits React's unopinionated nature for state and form management

## Consequences

### Positive

- Consistent project structure across all feature modules (fleet dashboard, equipment, work orders, parts, telemetry, AI insights)
- Built-in dependency injection simplifies service composition and testing
- Angular CLI accelerates development with schematics for components, services, guards, and interceptors
- TypeScript strict mode catches errors at compile time, reducing runtime defects
- Strong alignment with Kendo UI for Angular, which provides native Angular components

### Negative

- Developers without Angular experience will require onboarding time
- Angular's opinionated patterns may feel restrictive for developers accustomed to more flexible frameworks
- Upgrade path between major Angular versions requires periodic migration effort

### Risks

- Angular's release cadence (roughly annual major versions) means the team must plan for periodic upgrades
- If Google reduces investment in Angular, long-term support could become uncertain (mitigated by Angular's large open-source community)

## Implementation Notes

- Target ES2022 in `tsconfig.json` with `strict: true` enabled
- Use Angular CLI (`ng new`, `ng generate`) for all scaffolding
- Structure the application into feature modules: dashboard, equipment, work-orders, parts, telemetry, ai-insights, and shared
- Enable standalone components as the default component pattern (see ADR-0004)
- Configure `angular.json` with production build optimizations (AOT, tree-shaking, budgets)

## References

- L1-014: Responsive Design requirement
- Angular 17 documentation: https://angular.dev
- TypeScript 5.4 release notes: https://www.typescriptlang.org/docs/handbook/release-notes/typescript-5-4.html
