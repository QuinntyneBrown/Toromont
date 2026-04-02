// Acceptance Test
// Traces to: L2-021, L2-022
// Description: Verify report generation, chart display, and export

import { test, expect } from '@playwright/test';

test.describe('Reports', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/reports');
  });

  // L2-021 AC1: Fleet Utilization report configuration form
  test('displays report type selector cards', async ({ page }) => {
    await expect(page.locator('[data-testid="report-card-fleet-utilization"]')).toBeVisible();
    await expect(page.locator('[data-testid="report-card-maintenance-costs"]')).toBeVisible();
    await expect(page.locator('[data-testid="report-card-equipment-lifecycle"]')).toBeVisible();
  });

  test('Fleet Utilization report shows configuration form', async ({ page }) => {
    await page.locator('[data-testid="report-card-fleet-utilization"]').click();

    await expect(page.locator('[data-testid="date-range-picker"]')).toBeVisible();
    await expect(page.locator('[data-testid="equipment-filter"]')).toBeVisible();
    await expect(page.locator('[data-testid="generate-report-btn"]')).toBeVisible();
  });

  // L2-021 AC2: Generating report displays charts
  test('generates fleet utilization report with charts', async ({ page }) => {
    await page.locator('[data-testid="report-card-fleet-utilization"]').click();
    const dateRangePicker = page.locator('[data-testid="date-range-picker"]');
    await dateRangePicker.click();
    await page.locator('[data-testid="date-range-start"]').fill('2026-01-01');
    await page.locator('[data-testid="date-range-end"]').fill('2026-03-31');
    await page.locator('[data-testid="date-range-apply"]').click();
    await page.locator('[data-testid="generate-report-btn"]').click();

    await expect(page.locator('[data-testid="report-charts"]')).toBeVisible();
  });

  // L2-021 AC3-4: Export buttons present
  test('export buttons are available after report generation', async ({ page }) => {
    await page.locator('[data-testid="report-card-fleet-utilization"]').click();
    const dateRangePicker = page.locator('[data-testid="date-range-picker"]');
    await dateRangePicker.click();
    await page.locator('[data-testid="date-range-start"]').fill('2026-01-01');
    await page.locator('[data-testid="date-range-end"]').fill('2026-03-31');
    await page.locator('[data-testid="date-range-apply"]').click();
    await page.locator('[data-testid="generate-report-btn"]').click();

    await expect(page.locator('[data-testid="export-pdf-btn"]')).toBeVisible();
    await expect(page.locator('[data-testid="export-excel-btn"]')).toBeVisible();
    await expect(page.locator('[data-testid="export-csv-btn"]')).toBeVisible();
  });

  // L2-021 AC3: PDF export triggers download
  test('PDF export triggers file download', async ({ page }) => {
    await page.locator('[data-testid="report-card-fleet-utilization"]').click();
    const dateRangePicker = page.locator('[data-testid="date-range-picker"]');
    await dateRangePicker.click();
    await page.locator('[data-testid="date-range-start"]').fill('2026-01-01');
    await page.locator('[data-testid="date-range-end"]').fill('2026-03-31');
    await page.locator('[data-testid="date-range-apply"]').click();
    await page.locator('[data-testid="generate-report-btn"]').click();

    const downloadPromise = page.waitForEvent('download');
    await page.locator('[data-testid="export-pdf-btn"]').click();
    const download = await downloadPromise;
    expect(download.suggestedFilename()).toContain('.pdf');
  });

  // L2-022: Maintenance Costs report
  test('generates maintenance costs report', async ({ page }) => {
    await page.locator('[data-testid="report-card-maintenance-costs"]').click();

    await expect(page.locator('[data-testid="date-range-picker"]')).toBeVisible();
    await expect(page.locator('[data-testid="equipment-filter"]')).toBeVisible();
    const dateRangePicker = page.locator('[data-testid="date-range-picker"]');
    await dateRangePicker.click();
    await page.locator('[data-testid="date-range-start"]').fill('2026-01-01');
    await page.locator('[data-testid="date-range-end"]').fill('2026-03-31');
    await page.locator('[data-testid="date-range-apply"]').click();
    await page.locator('[data-testid="generate-report-btn"]').click();

    await expect(page.locator('[data-testid="report-charts"]')).toBeVisible();
  });
});
