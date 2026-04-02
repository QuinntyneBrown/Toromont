// Acceptance Tests — Service Management Redesign
// Traces to: L2-007, L2-008, L2-009
// Verifies: Frontend audit critical finding #2
// Intent: All tests FAIL before fix, PASS after fix

import { test, expect } from '@playwright/test';

test.describe('Work Order List — Kendo Grid', () => {

  test.beforeEach(async ({ page }) => {
    await page.goto('/service');
  });

  // FAIL: current impl uses plain <table>, not Kendo Grid
  test('work order list uses Kendo Grid component', async ({ page }) => {
    const kendoGrid = page.locator('[data-testid="work-orders-kendo-grid"]');
    await expect(kendoGrid).toBeVisible({ timeout: 5000 });

    // Verify Kendo Grid features are present
    await expect(kendoGrid.locator('.k-grid-header')).toBeVisible();
    await expect(kendoGrid.locator('.k-pager')).toBeVisible();
  });

  // FAIL: current impl does client-side pagination only
  test('grid pagination sends server-side request', async ({ page }) => {
    const apiCall = page.waitForRequest(
      req => req.url().includes('/work-orders') && req.url().includes('skip=')
    );

    const kendoGrid = page.locator('[data-testid="work-orders-kendo-grid"]');
    await expect(kendoGrid).toBeVisible();

    // Click next page in the pager
    await kendoGrid.locator('.k-pager .k-i-arrow-end-right, .k-pager-nav[title="Go to the next page"]').click();

    const request = await apiCall;
    expect(request.url()).toMatch(/skip=\d+/);
  });

  // FAIL: current impl has no column sort headers
  test('grid supports server-side sorting', async ({ page }) => {
    const apiCall = page.waitForRequest(
      req => req.url().includes('/work-orders') && req.url().includes('sort=')
    );

    // Click a column header to sort
    const header = page.locator('[data-testid="work-orders-kendo-grid"] .k-grid-header th').first();
    await header.click();

    const request = await apiCall;
    expect(request.url()).toContain('sort=');
  });
});

test.describe('Service Calendar — Kendo Scheduler', () => {

  test.beforeEach(async ({ page }) => {
    await page.goto('/service/calendar');
  });

  // FAIL: current impl is a flat card list, not Kendo Scheduler
  test('calendar uses Kendo Scheduler component', async ({ page }) => {
    const scheduler = page.locator('[data-testid="service-kendo-scheduler"]');
    await expect(scheduler).toBeVisible({ timeout: 5000 });

    // Verify scheduler has view switching buttons
    await expect(scheduler.locator('.k-scheduler-toolbar')).toBeVisible();
  });

  // FAIL: current impl does not call /work-orders/calendar endpoint
  test('calendar calls the calendar API endpoint with date range', async ({ page }) => {
    const apiCall = page.waitForRequest(
      req => req.url().includes('/work-orders/calendar')
        && req.url().includes('start=')
        && req.url().includes('end=')
    );

    const request = await apiCall;
    expect(request.url()).toContain('start=');
    expect(request.url()).toContain('end=');
  });

  // FAIL: current impl only has button toggles, no actual Scheduler views
  test('scheduler supports day/week/month view switching', async ({ page }) => {
    const scheduler = page.locator('[data-testid="service-kendo-scheduler"]');
    await expect(scheduler).toBeVisible();

    // Switch to week view
    await scheduler.locator('.k-scheduler-toolbar button, .k-view-week').click();
    await expect(scheduler.locator('.k-scheduler-weekview, .k-scheduler-body')).toBeVisible();

    // Switch to day view
    await scheduler.locator('.k-scheduler-toolbar button, .k-view-day').first().click();
    await expect(scheduler.locator('.k-scheduler-body')).toBeVisible();
  });
});

test.describe('Work Order History', () => {

  // FAIL: current impl calls /work-orders/{id}/history which 404s, then fabricates data
  test('work order detail shows real history from API', async ({ page }) => {
    await page.goto('/service');

    // Intercept the detail API call (includes history via Include)
    const detailCall = page.waitForRequest(
      req => req.url().match(/\/work-orders\/[a-f0-9-]+$/) !== null && req.method() === 'GET'
    );

    // Click first work order row
    await page.locator('[data-testid="work-order-row"], .k-grid-content tr').first().click();

    const request = await detailCall;
    const response = await request.response();
    const body = await response?.json();

    // Response should include history array
    expect(body.history).toBeDefined();
    expect(Array.isArray(body.history)).toBe(true);

    // UI should display the history
    const historyItems = page.locator('[data-testid="wo-history-item"]');
    await expect(historyItems.first()).toBeVisible({ timeout: 5000 });

    // History should NOT show 'System' as user (fabricated fallback)
    const firstUser = await historyItems.first()
      .locator('[data-testid="history-user"]').textContent();
    expect(firstUser?.trim()).not.toBe('System');
  });

  // FAIL: current impl does NOT call /work-orders/{id}/history
  test('history is NOT loaded from a separate /history endpoint', async ({ page }) => {
    await page.goto('/service');

    // Ensure NO request is made to /work-orders/{id}/history
    let historyEndpointCalled = false;
    page.on('request', req => {
      if (req.url().includes('/history')) {
        historyEndpointCalled = true;
      }
    });

    await page.locator('[data-testid="work-order-row"], .k-grid-content tr').first().click();
    await page.waitForTimeout(2000);

    expect(historyEndpointCalled).toBe(false);
  });
});

test.describe('Status Change Error Handling', () => {

  // FAIL: current impl updates UI even when API call fails
  test('failed status change does NOT update UI optimistically', async ({ page }) => {
    await page.goto('/service');

    // Mock the status update endpoint to fail
    await page.route('**/work-orders/*/status', route => {
      route.fulfill({ status: 422, body: JSON.stringify({ Error: 'Transition not allowed' }) });
    });

    await page.locator('[data-testid="work-order-row"], .k-grid-content tr').first().click();

    // Get current status
    const statusBefore = await page.locator('[data-testid="wo-status"]').first().textContent();

    // Try to start work (mock will fail)
    const startBtn = page.locator('[data-testid="action-start-work"]');
    if (await startBtn.isVisible()) {
      await startBtn.click();
      await page.waitForTimeout(1000);

      // Status should NOT have changed since API failed
      const statusAfter = await page.locator('[data-testid="wo-status"]').first().textContent();
      expect(statusAfter).toBe(statusBefore);
    }
  });
});
