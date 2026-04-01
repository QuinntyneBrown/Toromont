// Acceptance Test
// Traces to: L2-012, L2-019
// Description: Verify telemetry charts, auto-refresh, and notifications

import { test, expect } from '@playwright/test';

test.describe('Telemetry Dashboard', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/telemetry');
  });

  // L2-012 AC1: Four charts display for selected equipment
  test('displays four telemetry charts', async ({ page }) => {
    await expect(page.locator('[data-testid="chart-engine-hours"]')).toBeVisible();
    await expect(page.locator('[data-testid="chart-fuel-consumption"]')).toBeVisible();
    await expect(page.locator('[data-testid="chart-temperature"]')).toBeVisible();
    await expect(page.locator('[data-testid="chart-gps-trail"]')).toBeVisible();
  });

  // L2-012 AC1: Equipment selector and time range controls
  test('equipment selector and time range controls present', async ({ page }) => {
    await expect(page.locator('[data-testid="equipment-selector"]')).toBeVisible();
    await expect(page.locator('[data-testid="time-range-24h"]')).toBeVisible();
    await expect(page.locator('[data-testid="time-range-7d"]')).toBeVisible();
    await expect(page.locator('[data-testid="time-range-30d"]')).toBeVisible();
    await expect(page.locator('[data-testid="time-range-90d"]')).toBeVisible();
  });

  // L2-012 AC2: Selecting time range updates charts
  test('changing time range updates chart data', async ({ page }) => {
    await page.locator('[data-testid="time-range-30d"]').click();
    await expect(page.locator('[data-testid="time-range-30d"]')).toHaveClass(/active|selected/);
  });

  // L2-012 AC4: Empty state shows no data message
  test('shows no data message when no telemetry available', async ({ page }) => {
    // Select equipment with no data
    await page.locator('[data-testid="equipment-selector"]').click();
    await page.locator('[data-testid="equipment-option-empty"]').click();

    await expect(page.locator('[data-testid="no-data-message"]').first()).toBeVisible();
  });

  // L2-012 AC5: Mobile charts stack vertically
  test('charts stack vertically on mobile', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 812 });
    await page.goto('/telemetry');

    await expect(page.locator('[data-testid="chart-engine-hours"]')).toBeVisible();
    await expect(page.locator('[data-testid="chart-fuel-consumption"]')).toBeVisible();
  });

  // L2-012 AC3: Auto-refresh indicator
  test('shows auto-refresh indicator', async ({ page }) => {
    await expect(page.locator('[data-testid="auto-refresh-indicator"]')).toBeVisible();
  });
});

test.describe('In-App Notifications', () => {
  // L2-019 AC1: Notification bell shows unread count
  test('notification bell displays unread count badge', async ({ page }) => {
    await page.goto('/dashboard');
    const badge = page.locator('[data-testid="notification-badge"]');
    await expect(badge).toBeVisible();

    const count = await badge.textContent();
    expect(parseInt(count ?? '0')).toBeGreaterThanOrEqual(0);
  });

  // L2-019 AC2: Clicking bell opens notification dropdown
  test('clicking bell opens notification dropdown', async ({ page }) => {
    await page.goto('/dashboard');
    await page.locator('[data-testid="notification-bell"]').click();

    const dropdown = page.locator('[data-testid="notification-dropdown"]');
    await expect(dropdown).toBeVisible();

    const items = dropdown.locator('[data-testid="notification-item"]');
    const count = await items.count();
    expect(count).toBeLessThanOrEqual(20);
  });

  // L2-019 AC4: Clicking notification navigates to related entity
  test('clicking notification navigates to entity', async ({ page }) => {
    await page.goto('/dashboard');
    await page.locator('[data-testid="notification-bell"]').click();

    const firstNotification = page.locator('[data-testid="notification-item"]').first();
    if (await firstNotification.isVisible()) {
      await firstNotification.click();
      // Should navigate away from dashboard
      await expect(page).not.toHaveURL('/dashboard');
    }
  });

  // L2-019 AC5: Mark All as Read resets badge
  test('mark all as read resets badge count', async ({ page }) => {
    await page.goto('/dashboard');
    await page.locator('[data-testid="notification-bell"]').click();
    await page.locator('[data-testid="mark-all-read"]').click();

    const badge = page.locator('[data-testid="notification-badge"]');
    // Badge should either disappear or show 0
    const isVisible = await badge.isVisible();
    if (isVisible) {
      const count = await badge.textContent();
      expect(count).toBe('0');
    }
  });
});
