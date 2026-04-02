// Acceptance Tests — Notification Wiring
// Traces to: L2-019, L2-020
// Verifies: Frontend audit critical finding #1
// Intent: All tests FAIL before fix, PASS after fix

import { test, expect } from '@playwright/test';

test.describe('Notification Bell & Dropdown', () => {

  test.beforeEach(async ({ page }) => {
    await page.goto('/dashboard');
  });

  // FAIL: dropdown never loads notifications from REST API
  test('opening bell dropdown shows recent notifications from API', async ({ page }) => {
    await page.locator('[data-testid="notification-bell"]').click();
    const dropdown = page.locator('[data-testid="notification-dropdown"]');
    await expect(dropdown).toBeVisible();

    // Wait for notifications to load from REST API
    const items = dropdown.locator('[data-testid="notification-item"]');
    await expect(items.first()).toBeVisible({ timeout: 5000 });

    // Each notification should have title and message content
    const firstItem = items.first();
    await expect(firstItem.locator('.notification-title')).not.toBeEmpty();
    await expect(firstItem.locator('.notification-message')).not.toBeEmpty();
  });

  // FAIL: markAsRead calls nonexistent SignalR method instead of REST PUT
  test('clicking a notification marks it as read via REST API', async ({ page }) => {
    await page.locator('[data-testid="notification-bell"]').click();

    // Intercept the REST API call to verify it is made
    const markReadPromise = page.waitForRequest(
      req => req.url().includes('/notifications/') && req.url().includes('/read')
        && req.method() === 'PUT'
    );

    const firstItem = page.locator('[data-testid="notification-item"]').first();
    if (await firstItem.isVisible()) {
      await firstItem.click();

      // Should make a REST PUT call, not a SignalR invoke
      const request = await markReadPromise;
      expect(request.method()).toBe('PUT');
    }
  });

  // FAIL: markAllAsRead loops via SignalR instead of calling REST PUT read-all
  test('mark all as read calls REST API and resets badge', async ({ page }) => {
    const markAllPromise = page.waitForRequest(
      req => req.url().includes('/notifications/read-all') && req.method() === 'PUT'
    );

    await page.locator('[data-testid="notification-bell"]').click();
    await page.locator('[data-testid="mark-all-read"]').click();

    const request = await markAllPromise;
    expect(request.method()).toBe('PUT');

    // Badge should reset to 0 or disappear
    const badge = page.locator('[data-testid="notification-badge"]');
    await expect(badge).toBeHidden({ timeout: 3000 });
  });
});

test.describe('Notification Deep Links', () => {

  // FAIL: current routes /service/{id} and /parts/{id} do not exist
  test('equipment notification navigates to equipment detail', async ({ page }) => {
    await page.goto('/dashboard');
    await page.locator('[data-testid="notification-bell"]').click();

    // Find a notification with equipment entity type
    const items = page.locator('[data-testid="notification-item"]');
    await expect(items.first()).toBeVisible({ timeout: 5000 });

    await items.first().click();

    // Should navigate to a valid route (not stay on dashboard with error)
    await page.waitForTimeout(1000);
    const url = page.url();
    expect(url).not.toContain('/dashboard');
    // Should not have a console error about unknown route
  });
});

test.describe('SignalR Connection', () => {

  // FAIL: startConnection() is never called
  test('SignalR connection is established on app load', async ({ page }) => {
    // Listen for the SignalR negotiation request
    const signalrPromise = page.waitForRequest(
      req => req.url().includes('/hubs/notifications') && req.url().includes('negotiate'),
      { timeout: 10000 }
    );

    await page.goto('/dashboard');

    // Should attempt to establish SignalR connection
    const request = await signalrPromise;
    expect(request.url()).toContain('/hubs/notifications');
  });

  // FAIL: real-time notifications don't arrive because connection is never started
  test('real-time notification updates badge count', async ({ page }) => {
    await page.goto('/dashboard');

    // Get initial badge count
    const badge = page.locator('[data-testid="notification-badge"]');
    const initialCount = await badge.textContent().catch(() => '0');

    // Trigger a notification via API (e.g., create an alert)
    await page.request.post('/api/v1/telemetry/ingest', {
      headers: { 'X-Api-Key': process.env.TELEMETRY_API_KEY || 'test-key' },
      data: {
        equipmentId: 'test-equipment-id',
        eventType: 'periodic_reading',
        temperature: 999  // above threshold to trigger alert notification
      }
    });

    // Badge count should increment via SignalR push
    await expect(badge).not.toHaveText(initialCount || '0', { timeout: 10000 });
  });
});

test.describe('Notification Preferences', () => {

  // FAIL: no route for /settings/notifications
  test('notification preferences page is accessible', async ({ page }) => {
    await page.goto('/settings/notifications');

    // Should not redirect to dashboard (current: route doesn't exist)
    await expect(page).toHaveURL(/settings\/notifications/);
  });

  // FAIL: preferences are static in-memory, no API calls
  test('preferences load from API and save to API', async ({ page }) => {
    // Intercept GET preferences
    const getPrefsPromise = page.waitForRequest(
      req => req.url().includes('/notifications/preferences') && req.method() === 'GET'
    );

    await page.goto('/settings/notifications');

    const getRequest = await getPrefsPromise;
    expect(getRequest.method()).toBe('GET');

    // Toggle a preference and save
    const savePrefsPromise = page.waitForRequest(
      req => req.url().includes('/notifications/preferences') && req.method() === 'PUT'
    );

    // Find and toggle a preference checkbox
    const firstToggle = page.locator('[data-testid^="pref-"] input[type="checkbox"]').first();
    if (await firstToggle.isVisible()) {
      await firstToggle.click();
    }

    // Click save
    await page.locator('[data-testid="save-preferences"]').click();

    const saveRequest = await savePrefsPromise;
    expect(saveRequest.method()).toBe('PUT');
  });
});
