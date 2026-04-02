# ADR-0002: Bootstrap 5 with SCSS and CSS Custom Properties for Styling

**Date:** 2026-04-01
**Category:** frontend
**Status:** Accepted
**Deciders:** Ironvale Fleet Hub Architecture Team

## Context

Ironvale Fleet Hub must provide a responsive user interface that works across desktop monitors in fleet operations centers, tablets used by field technicians, and mobile phones for on-the-go management. The styling system must support five breakpoints (XS <576px, S >=576px, M >=768px, L >=992px, XL >=1200px) and handle complex layouts including persistent sidebars, data-heavy dashboards, equipment grids, and work order calendars. The team needs a styling approach that is maintainable, customizable to Ironvale branding, and accessible by default.

## Decision

Use Bootstrap 5 with SCSS preprocessing and CSS custom properties using the oklch color space for theming and responsive layout. Bootstrap's grid system and utility classes provide the responsive foundation, while SCSS variables and mixins enable Ironvale-specific customization.

## Options Considered

### Option 1: Bootstrap 5 with SCSS (chosen)

- **Pros:**
  - Mature, battle-tested responsive grid system with 5 configurable breakpoints
  - Extensive pre-built component library (navbars, modals, cards, dropdowns, forms)
  - SCSS source enables deep customization of variables, mixins, and component styles
  - CSS custom properties support runtime theming (light/dark mode, branding)
  - oklch color space provides perceptually uniform color manipulation for accessible contrast ratios
  - Widely known by frontend developers, reducing onboarding time
  - Good accessibility defaults (ARIA attributes, focus management, color contrast)
  - No jQuery dependency in Bootstrap 5

- **Cons:**
  - Larger CSS bundle if not carefully tree-shaken (mitigated by importing only needed modules)
  - Generic look-and-feel requires customization effort to match Ironvale branding
  - Some Bootstrap components may overlap with Kendo UI components, requiring clear usage guidelines

### Option 2: Tailwind CSS

- **Pros:**
  - Utility-first approach produces small, optimized CSS bundles
  - Highly customizable design tokens
  - Growing ecosystem and community

- **Cons:**
  - Verbose HTML with many utility classes reduces template readability
  - No pre-built components; everything must be composed from utilities
  - Less conventional in Angular enterprise projects
  - Steeper learning curve for developers accustomed to component-based CSS frameworks

### Option 3: Angular Material

- **Pros:**
  - Native Angular components with built-in accessibility
  - Material Design provides a consistent visual language
  - Tight integration with Angular CDK

- **Cons:**
  - Material Design aesthetic may not align with Ironvale branding requirements
  - Customization beyond Material theming is complex
  - Overlap with Kendo UI components would create confusion about which library to use for which component

### Option 4: Custom CSS Framework

- **Pros:**
  - Complete control over every aspect of styling
  - No external dependencies
  - Perfectly tailored to Ironvale requirements

- **Cons:**
  - Significant upfront development effort to build grid system, components, and utilities
  - Ongoing maintenance burden for responsive behavior, accessibility, and browser compatibility
  - Reinvents solved problems

## Consequences

### Positive

- Responsive layouts work across all five breakpoints with minimal custom CSS
- Developers can leverage familiar Bootstrap classes for rapid UI development
- SCSS variables centralize Ironvale branding (colors, typography, spacing) for consistent theming
- CSS custom properties with oklch enable perceptually uniform color scales and accessible contrast ratios
- Bootstrap's responsive utilities simplify breakpoint-specific visibility and layout changes

### Negative

- Team must establish clear guidelines on when to use Bootstrap components vs. Kendo UI components to avoid duplication
- Custom SCSS overrides must be maintained alongside Bootstrap upgrades
- oklch color space requires fallback values for older browsers that lack support

### Risks

- Over-reliance on Bootstrap defaults could result in a generic appearance if customization is not prioritized
- Mixing Bootstrap grid with Kendo UI layout components may cause unexpected behavior if not carefully managed

## Implementation Notes

- Import Bootstrap SCSS source selectively in `styles.scss` (grid, utilities, reboot, forms, navbars, cards, modals) rather than the full bundle
- Define Ironvale brand colors as SCSS variables mapped to CSS custom properties using oklch:
  - `--toro-primary`, `--toro-secondary`, `--toro-accent`, `--toro-danger`, `--toro-warning`, `--toro-success`
- Responsive sidebar behavior:
  - **L and XL (>=992px):** Sidebar is persistent and fully expanded with text labels
  - **M (>=768px):** Sidebar collapses to icons-only rail
  - **S and XS (<768px):** Sidebar is hidden behind a hamburger menu toggle
- Data table responsive pattern: full table layout on M+ breakpoints; card-based stacked layout on S/XS breakpoints
- Use Bootstrap's `container-fluid` for dashboard layouts and `container` for form-based pages
- Configure `angular.json` to include Bootstrap SCSS in the build pipeline

## References

- L1-014: Responsive Design requirement
- Bootstrap 5 documentation: https://getbootstrap.com/docs/5.3
- oklch color space: https://developer.mozilla.org/en-US/docs/Web/CSS/color_value/oklch
