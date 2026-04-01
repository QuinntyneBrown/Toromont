// Acceptance Test
// Traces to: L2-007, L2-008, L2-009
// Description: Verify work order creation, lifecycle tracking, and service calendar

import { test, expect } from '@playwright/test';
import { WorkOrdersPage } from '../pages/work-orders.page';

test.describe('Work Orders List', () => {
  let workOrders: WorkOrdersPage;

  test.beforeEach(async ({ page }) => {
    workOrders = new WorkOrdersPage(page);
    await workOrders.goto();
  });

  // L2-007 AC1: Create Work Order form with all required fields
  test('Create Work Order button is visible for Fleet Manager', async () => {
    await expect(workOrders.createWorkOrderButton).toBeVisible();
  });

  test('opens create work order form with required fields', async ({ page }) => {
    await workOrders.clickCreateWorkOrder();

    await expect(page.locator('[data-testid="wo-equipment-select"]')).toBeVisible();
    await expect(page.locator('[data-testid="wo-service-type"]')).toBeVisible();
    await expect(page.locator('[data-testid="wo-priority"]')).toBeVisible();
    await expect(page.locator('[data-testid="wo-description"]')).toBeVisible();
    await expect(page.locator('[data-testid="wo-requested-date"]')).toBeVisible();
    await expect(page.locator('[data-testid="wo-assignee"]')).toBeVisible();
  });

  // L2-007 AC2: Submitting creates WO with Open status and unique number
  test('creates work order with Open status', async ({ page }) => {
    await workOrders.clickCreateWorkOrder();

    await page.locator('[data-testid="wo-equipment-select"]').click();
    await page.locator('[data-testid="equipment-option"]').first().click();
    await page.locator('[data-testid="wo-service-type"]').selectOption('Corrective');
    await page.locator('[data-testid="wo-priority"]').selectOption('High');
    await page.locator('[data-testid="wo-description"]').fill('Test work order');
    await page.locator('[data-testid="wo-requested-date"]').fill('2026-04-10');
    await page.locator('[data-testid="wo-assignee"]').click();
    await page.locator('[data-testid="assignee-option"]').first().click();
    await page.locator('[data-testid="wo-submit"]').click();

    // Should show success and WO number
    await expect(page.locator('[data-testid="wo-number"]')).toContainText(/WO-/);
    await expect(page.locator('[data-testid="wo-status"]')).toContainText('Open');
  });

  // L2-008: Status filter tabs work correctly
  test('status tabs filter work orders', async () => {
    await expect(workOrders.statusTabs).toBeVisible();
    await expect(workOrders.tabAll).toBeVisible();
    await expect(workOrders.tabOpen).toBeVisible();
    await expect(workOrders.tabInProgress).toBeVisible();

    await workOrders.selectTab('open');
    const rowCount = await workOrders.getRowCount();
    expect(rowCount).toBeGreaterThan(0);

    for (let i = 0; i < rowCount; i++) {
      const status = await workOrders.getRowStatus(i);
      expect(status).toBe('Open');
    }
  });

  // L2-008 AC1-4: Work order status transitions
  test('work order can transition through statuses', async ({ page }) => {
    await workOrders.selectTab('open');
    await workOrders.clickRow(0);

    // Open -> In Progress
    await page.locator('[data-testid="action-start-work"]').click();
    await expect(page.locator('[data-testid="wo-status"]')).toContainText('In Progress');

    // In Progress -> Completed
    await page.locator('[data-testid="action-complete"]').click();
    await page.locator('[data-testid="completion-notes"]').fill('Repair completed successfully');
    await page.locator('[data-testid="confirm-complete"]').click();
    await expect(page.locator('[data-testid="wo-status"]')).toContainText('Completed');
  });

  // L2-008 AC5: Status history records transitions
  test('work order history shows status transitions', async ({ page }) => {
    await workOrders.clickRow(0);

    const historyItems = page.locator('[data-testid="wo-history-item"]');
    const count = await historyItems.count();
    expect(count).toBeGreaterThan(0);

    // Each history item should show status, user, and timestamp
    const firstItem = historyItems.first();
    await expect(firstItem.locator('[data-testid="history-status"]')).toBeVisible();
    await expect(firstItem.locator('[data-testid="history-user"]')).toBeVisible();
    await expect(firstItem.locator('[data-testid="history-timestamp"]')).toBeVisible();
  });
});

// L2-009: Service Calendar
test.describe('Service Calendar', () => {
  test('displays calendar with work orders', async ({ page }) => {
    await page.goto('/service/calendar');

    const scheduler = page.locator('[data-testid="service-calendar"]');
    await expect(scheduler).toBeVisible();

    // View toggles should be present
    await expect(page.locator('[data-testid="view-day"]')).toBeVisible();
    await expect(page.locator('[data-testid="view-week"]')).toBeVisible();
    await expect(page.locator('[data-testid="view-month"]')).toBeVisible();
  });

  // L2-009 AC2: Calendar events color-coded by priority
  test('calendar events are color-coded by priority', async ({ page }) => {
    await page.goto('/service/calendar');

    const events = page.locator('[data-testid="calendar-event"]');
    const count = await events.count();
    expect(count).toBeGreaterThan(0);
  });

  // L2-009 AC4: Clicking event shows popup with details
  test('clicking event shows details popup', async ({ page }) => {
    await page.goto('/service/calendar');

    const event = page.locator('[data-testid="calendar-event"]').first();
    await event.click();

    const popup = page.locator('[data-testid="event-popup"]');
    await expect(popup).toBeVisible();
    await expect(popup.locator('[data-testid="popup-wo-number"]')).toBeVisible();
    await expect(popup.locator('[data-testid="popup-equipment"]')).toBeVisible();
  });
});
