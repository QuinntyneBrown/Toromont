# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: reports.spec.ts >> Reports >> generates maintenance costs report
- Location: tests\reports.spec.ts:72:7

# Error details

```
Test timeout of 30000ms exceeded.
```

```
Error: locator.click: Test timeout of 30000ms exceeded.
Call log:
  - waiting for locator('[data-testid="report-card-maintenance-costs"]')

```

# Test source

```ts
  1  | // Acceptance Test
  2  | // Traces to: L2-021, L2-022
  3  | // Description: Verify report generation, chart display, and export
  4  | 
  5  | import { test, expect } from '@playwright/test';
  6  | 
  7  | test.describe('Reports', () => {
  8  |   test.beforeEach(async ({ page }) => {
  9  |     await page.goto('/reports');
  10 |   });
  11 | 
  12 |   // L2-021 AC1: Fleet Utilization report configuration form
  13 |   test('displays report type selector cards', async ({ page }) => {
  14 |     await expect(page.locator('[data-testid="report-card-fleet-utilization"]')).toBeVisible();
  15 |     await expect(page.locator('[data-testid="report-card-maintenance-costs"]')).toBeVisible();
  16 |     await expect(page.locator('[data-testid="report-card-equipment-lifecycle"]')).toBeVisible();
  17 |   });
  18 | 
  19 |   test('Fleet Utilization report shows configuration form', async ({ page }) => {
  20 |     await page.locator('[data-testid="report-card-fleet-utilization"]').click();
  21 | 
  22 |     await expect(page.locator('[data-testid="date-range-picker"]')).toBeVisible();
  23 |     await expect(page.locator('[data-testid="equipment-filter"]')).toBeVisible();
  24 |     await expect(page.locator('[data-testid="generate-report-btn"]')).toBeVisible();
  25 |   });
  26 | 
  27 |   // L2-021 AC2: Generating report displays charts
  28 |   test('generates fleet utilization report with charts', async ({ page }) => {
  29 |     await page.locator('[data-testid="report-card-fleet-utilization"]').click();
  30 |     const dateRangePicker = page.locator('[data-testid="date-range-picker"]');
  31 |     await dateRangePicker.click();
  32 |     await page.locator('[data-testid="date-range-start"]').fill('2026-01-01');
  33 |     await page.locator('[data-testid="date-range-end"]').fill('2026-03-31');
  34 |     await page.locator('[data-testid="date-range-apply"]').click();
  35 |     await page.locator('[data-testid="generate-report-btn"]').click();
  36 | 
  37 |     await expect(page.locator('[data-testid="report-charts"]')).toBeVisible();
  38 |   });
  39 | 
  40 |   // L2-021 AC3-4: Export buttons present
  41 |   test('export buttons are available after report generation', async ({ page }) => {
  42 |     await page.locator('[data-testid="report-card-fleet-utilization"]').click();
  43 |     const dateRangePicker = page.locator('[data-testid="date-range-picker"]');
  44 |     await dateRangePicker.click();
  45 |     await page.locator('[data-testid="date-range-start"]').fill('2026-01-01');
  46 |     await page.locator('[data-testid="date-range-end"]').fill('2026-03-31');
  47 |     await page.locator('[data-testid="date-range-apply"]').click();
  48 |     await page.locator('[data-testid="generate-report-btn"]').click();
  49 | 
  50 |     await expect(page.locator('[data-testid="export-pdf-btn"]')).toBeVisible();
  51 |     await expect(page.locator('[data-testid="export-excel-btn"]')).toBeVisible();
  52 |     await expect(page.locator('[data-testid="export-csv-btn"]')).toBeVisible();
  53 |   });
  54 | 
  55 |   // L2-021 AC3: PDF export triggers download
  56 |   test('PDF export triggers file download', async ({ page }) => {
  57 |     await page.locator('[data-testid="report-card-fleet-utilization"]').click();
  58 |     const dateRangePicker = page.locator('[data-testid="date-range-picker"]');
  59 |     await dateRangePicker.click();
  60 |     await page.locator('[data-testid="date-range-start"]').fill('2026-01-01');
  61 |     await page.locator('[data-testid="date-range-end"]').fill('2026-03-31');
  62 |     await page.locator('[data-testid="date-range-apply"]').click();
  63 |     await page.locator('[data-testid="generate-report-btn"]').click();
  64 | 
  65 |     const downloadPromise = page.waitForEvent('download');
  66 |     await page.locator('[data-testid="export-pdf-btn"]').click();
  67 |     const download = await downloadPromise;
  68 |     expect(download.suggestedFilename()).toContain('.pdf');
  69 |   });
  70 | 
  71 |   // L2-022: Maintenance Costs report
  72 |   test('generates maintenance costs report', async ({ page }) => {
> 73 |     await page.locator('[data-testid="report-card-maintenance-costs"]').click();
     |                                                                         ^ Error: locator.click: Test timeout of 30000ms exceeded.
  74 | 
  75 |     await expect(page.locator('[data-testid="date-range-picker"]')).toBeVisible();
  76 |     await expect(page.locator('[data-testid="equipment-filter"]')).toBeVisible();
  77 |     const dateRangePicker = page.locator('[data-testid="date-range-picker"]');
  78 |     await dateRangePicker.click();
  79 |     await page.locator('[data-testid="date-range-start"]').fill('2026-01-01');
  80 |     await page.locator('[data-testid="date-range-end"]').fill('2026-03-31');
  81 |     await page.locator('[data-testid="date-range-apply"]').click();
  82 |     await page.locator('[data-testid="generate-report-btn"]').click();
  83 | 
  84 |     await expect(page.locator('[data-testid="report-charts"]')).toBeVisible();
  85 |   });
  86 | });
  87 | 
```