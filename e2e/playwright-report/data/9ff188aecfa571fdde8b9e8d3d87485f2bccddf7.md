# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: ai-insights.spec.ts >> AI Insights Dashboard >> prediction row shows all required fields
- Location: tests\ai-insights.spec.ts:39:7

# Error details

```
Error: expect(locator).toBeVisible() failed

Locator: locator('[data-testid="prediction-row"]').first().locator('[data-testid="pred-equipment"]')
Expected: visible
Timeout: 5000ms
Error: element(s) not found

Call log:
  - Expect "toBeVisible" with timeout 5000ms
  - waiting for locator('[data-testid="prediction-row"]').first().locator('[data-testid="pred-equipment"]')

```

# Test source

```ts
  1  | // Acceptance Test
  2  | // Traces to: L2-014, L2-016
  3  | // Description: Verify AI predictions dashboard and anomaly detection alerts
  4  | 
  5  | import { test, expect } from '@playwright/test';
  6  | 
  7  | test.describe('AI Insights Dashboard', () => {
  8  |   test.beforeEach(async ({ page }) => {
  9  |     await page.goto('/ai-insights');
  10 |   });
  11 | 
  12 |   // L2-014 AC3: Predictions listed in sortable grid
  13 |   test('displays predictions in a sortable grid', async ({ page }) => {
  14 |     const grid = page.locator('[data-testid="predictions-grid"]');
  15 |     await expect(grid).toBeVisible();
  16 | 
  17 |     const rows = page.locator('[data-testid="prediction-row"]');
  18 |     const count = await rows.count();
  19 |     expect(count).toBeGreaterThan(0);
  20 |   });
  21 | 
  22 |   // L2-014: KPI summary cards
  23 |   test('displays AI KPI summary cards', async ({ page }) => {
  24 |     await expect(page.locator('[data-testid="kpi-total-predictions"]')).toBeVisible();
  25 |     await expect(page.locator('[data-testid="kpi-high-priority"]')).toBeVisible();
  26 |     await expect(page.locator('[data-testid="kpi-anomalies"]')).toBeVisible();
  27 |     await expect(page.locator('[data-testid="kpi-cost-savings"]')).toBeVisible();
  28 |   });
  29 | 
  30 |   // L2-014 AC4: High confidence predictions have High Priority badge
  31 |   test('high confidence predictions show High Priority badge', async ({ page }) => {
  32 |     const highPriorityBadge = page.locator('[data-testid="prediction-row"] [data-testid="priority-badge"]:has-text("High")');
  33 |     if (await highPriorityBadge.count() > 0) {
  34 |       await expect(highPriorityBadge.first()).toBeVisible();
  35 |     }
  36 |   });
  37 | 
  38 |   // L2-014 AC2: Prediction shows component, confidence, action, timeframe
  39 |   test('prediction row shows all required fields', async ({ page }) => {
  40 |     const firstRow = page.locator('[data-testid="prediction-row"]').first();
> 41 |     await expect(firstRow.locator('[data-testid="pred-equipment"]')).toBeVisible();
     |                                                                      ^ Error: expect(locator).toBeVisible() failed
  42 |     await expect(firstRow.locator('[data-testid="pred-component"]')).toBeVisible();
  43 |     await expect(firstRow.locator('[data-testid="pred-confidence"]')).toBeVisible();
  44 |     await expect(firstRow.locator('[data-testid="pred-timeframe"]')).toBeVisible();
  45 |   });
  46 | 
  47 |   // L2-016 AC4: Anomaly alerts displayed with required info
  48 |   test('anomaly alerts panel shows alerts with details', async ({ page }) => {
  49 |     const anomalyPanel = page.locator('[data-testid="anomaly-alerts"]');
  50 |     await expect(anomalyPanel).toBeVisible();
  51 | 
  52 |     const alerts = page.locator('[data-testid="anomaly-alert"]');
  53 |     if (await alerts.count() > 0) {
  54 |       const firstAlert = alerts.first();
  55 |       await expect(firstAlert.locator('[data-testid="anomaly-type"]')).toBeVisible();
  56 |       await expect(firstAlert.locator('[data-testid="anomaly-equipment"]')).toBeVisible();
  57 |       await expect(firstAlert.locator('[data-testid="anomaly-severity"]')).toBeVisible();
  58 |     }
  59 |   });
  60 | });
  61 | 
```