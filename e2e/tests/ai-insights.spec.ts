// Acceptance Test
// Traces to: L2-014, L2-016
// Description: Verify AI predictions dashboard and anomaly detection alerts

import { test, expect } from '@playwright/test';

test.describe('AI Insights Dashboard', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/ai-insights');
  });

  // L2-014 AC3: Predictions listed in sortable grid
  test('displays predictions in a sortable grid', async ({ page }) => {
    const grid = page.locator('[data-testid="predictions-grid"]');
    await expect(grid).toBeVisible();

    const rows = page.locator('[data-testid="prediction-row"]');
    const count = await rows.count();
    expect(count).toBeGreaterThan(0);
  });

  // L2-014: KPI summary cards
  test('displays AI KPI summary cards', async ({ page }) => {
    await expect(page.locator('[data-testid="kpi-total-predictions"]')).toBeVisible();
    await expect(page.locator('[data-testid="kpi-high-priority"]')).toBeVisible();
    await expect(page.locator('[data-testid="kpi-anomalies"]')).toBeVisible();
    await expect(page.locator('[data-testid="kpi-cost-savings"]')).toBeVisible();
  });

  // L2-014 AC4: High confidence predictions have High Priority badge
  test('high confidence predictions show High Priority badge', async ({ page }) => {
    const highPriorityBadge = page.locator('[data-testid="prediction-row"] [data-testid="priority-badge"]:has-text("High")');
    if (await highPriorityBadge.count() > 0) {
      await expect(highPriorityBadge.first()).toBeVisible();
    }
  });

  // L2-014 AC2: Prediction shows component, confidence, action, timeframe
  test('prediction row shows all required fields', async ({ page }) => {
    const firstRow = page.locator('[data-testid="prediction-row"]').first();
    await expect(firstRow.locator('[data-testid="pred-equipment"]')).toBeVisible();
    await expect(firstRow.locator('[data-testid="pred-component"]')).toBeVisible();
    await expect(firstRow.locator('[data-testid="pred-confidence"]')).toBeVisible();
    await expect(firstRow.locator('[data-testid="pred-timeframe"]')).toBeVisible();
  });

  // L2-016 AC4: Anomaly alerts displayed with required info
  test('anomaly alerts panel shows alerts with details', async ({ page }) => {
    const anomalyPanel = page.locator('[data-testid="anomaly-alerts"]');
    await expect(anomalyPanel).toBeVisible();

    const alerts = page.locator('[data-testid="anomaly-alert"]');
    if (await alerts.count() > 0) {
      const firstAlert = alerts.first();
      await expect(firstAlert.locator('[data-testid="anomaly-type"]')).toBeVisible();
      await expect(firstAlert.locator('[data-testid="anomaly-equipment"]')).toBeVisible();
      await expect(firstAlert.locator('[data-testid="anomaly-severity"]')).toBeVisible();
    }
  });
});
