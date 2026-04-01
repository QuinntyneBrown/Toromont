# ADR-0004: Standalone Components Architecture

**Date:** 2026-04-01
**Category:** frontend
**Status:** Accepted
**Deciders:** Toromont Fleet Hub Architecture Team

## Context

Angular 17 introduced stable standalone APIs as the recommended approach for building Angular applications. Historically, Angular required NgModules to declare components, directives, and pipes, and to configure dependency injection. This module-based architecture added significant boilerplate, created indirection between component usage and declarations, and complicated lazy loading. With standalone components, each component self-declares its imports, making the dependency graph explicit and reducing the need for NgModule orchestration. The Toromont Fleet Hub project is a greenfield application, providing the opportunity to adopt standalone components from the start.

## Decision

Use Angular 17 standalone components as the default component architecture pattern instead of the traditional NgModule-based approach. All components, directives, and pipes will be declared with `standalone: true`. The application bootstrap will use `bootstrapApplication()` with `provideRouter()` and other function-based providers rather than an `AppModule`.

## Options Considered

### Option 1: Standalone Components (chosen)

- **Pros:**
  - Reduced boilerplate: no need to create, maintain, or update NgModule `declarations` and `imports` arrays
  - Better tree-shaking: the compiler can trace exact dependencies per component, eliminating unused code more effectively
  - Simpler lazy loading: routes can directly reference standalone components with `loadComponent` without requiring a wrapper module
  - Self-documenting dependencies: each component's `imports` array shows exactly what it uses
  - Angular team's officially recommended path forward for all new projects
  - Simpler mental model for developers: components are the primary building block, not modules
  - Easier to refactor and move components between features

- **Cons:**
  - Developers familiar with NgModule patterns may need time to adjust
  - Some third-party libraries may still expect NgModule-based configuration (though Kendo UI supports standalone)
  - Repeated imports in components that share many dependencies (mitigated by creating shared import arrays)

### Option 2: Traditional NgModule Architecture

- **Pros:**
  - Well-established pattern with extensive documentation and community examples
  - Clear organizational boundaries via feature modules
  - Familiar to Angular developers with prior experience

- **Cons:**
  - Significant boilerplate for module declarations, imports, and exports
  - Indirect dependency graph: must trace through module imports to understand what a component can use
  - Lazy loading requires wrapper modules with `loadChildren`
  - Angular team has signaled NgModules will become optional and eventually deprecated

### Option 3: Hybrid Approach (Standalone + NgModules)

- **Pros:**
  - Allows gradual adoption of standalone components
  - Can use NgModules where third-party libraries require them
  - Flexibility to choose per feature

- **Cons:**
  - Inconsistent patterns across the codebase create confusion
  - Developers must understand both paradigms
  - Code reviews become more complex when two patterns coexist
  - Defeats the purpose of simplification if both approaches are actively used

## Consequences

### Positive

- Consistent, minimal boilerplate across all feature areas (dashboard, equipment, work orders, parts, telemetry, AI insights)
- Lazy loading is straightforward with `loadComponent` in route definitions, improving initial load performance
- New developers can understand component dependencies by reading the component file alone
- Tree-shaking is maximally effective, keeping production bundle sizes optimized
- Aligns with Angular's long-term roadmap, reducing future migration effort

### Negative

- Components with many shared dependencies (e.g., multiple Kendo modules, common pipes, shared directives) will have verbose import arrays
- Developers experienced only with NgModule-based Angular will need to learn the standalone patterns
- Some Angular documentation and tutorials still reference NgModule-based examples, which may cause confusion

### Risks

- If a critical third-party library does not support standalone component consumption, a compatibility wrapper may be needed
- The standalone API, while stable in Angular 17, continues to evolve; future Angular versions may introduce breaking changes to provider functions

## Implementation Notes

- Bootstrap the application with `bootstrapApplication(AppComponent, { providers: [...] })` in `main.ts`
- `AppComponent` is declared with `standalone: true` and imports `RouterOutlet`
- Configure providers using function-based APIs:
  - `provideRouter(routes)` with `withComponentInputBinding()` for route parameter binding
  - `provideHttpClient(withInterceptorsFromDi())` for HTTP with MSAL interceptor support
  - `provideAnimationsAsync()` for Kendo UI animation support
- Route definitions use `loadComponent` for lazy loading:
  ```typescript
  { path: 'equipment', loadComponent: () => import('./features/equipment/equipment.component').then(m => m.EquipmentComponent) }
  ```
- Create shared import arrays for commonly used dependencies:
  ```typescript
  export const KENDO_GRID_IMPORTS = [GridModule, PDFModule, ExcelModule];
  export const COMMON_IMPORTS = [CommonModule, ReactiveFormsModule, RouterLink];
  ```
- All generated components via Angular CLI will use the `--standalone` flag (default in Angular 17)

## References

- L1-014: Responsive Design requirement
- Angular Standalone Components guide: https://angular.dev/guide/components/importing
- Angular bootstrapApplication API: https://angular.dev/api/platform-browser/bootstrapApplication
