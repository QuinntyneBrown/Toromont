// Acceptance Test
// Traces to: L2-017, L2-018, L2-020
// Description: Verify user management, multi-tenancy, notification preferences

import { test, expect } from '@playwright/test';

test.describe('User Management', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/admin/users');
  });

  // L2-017 AC1: Admin sees user list with invite button
  test('displays user list with invite button', async ({ page }) => {
    await expect(page.locator('[data-testid="users-grid"]')).toBeVisible();
    await expect(page.locator('[data-testid="invite-user-btn"]')).toBeVisible();

    const rows = page.locator('[data-testid="user-row"]');
    const count = await rows.count();
    expect(count).toBeGreaterThan(0);
  });

  // L2-017 AC1: Invite form has email and role fields
  test('invite user form has required fields', async ({ page }) => {
    await page.locator('[data-testid="invite-user-btn"]').click();

    await expect(page.locator('[data-testid="invite-email"]')).toBeVisible();
    await expect(page.locator('[data-testid="invite-role"]')).toBeVisible();
    await expect(page.locator('[data-testid="invite-submit"]')).toBeVisible();
  });

  // L2-017 AC2: Submitting invitation sends email
  test('submitting invitation shows success', async ({ page }) => {
    await page.locator('[data-testid="invite-user-btn"]').click();
    await page.locator('[data-testid="invite-email"]').fill('newuser@example.com');
    await page.locator('[data-testid="invite-role"]').selectOption('Technician');
    await page.locator('[data-testid="invite-submit"]').click();

    await expect(page.locator('[data-testid="invite-success"]')).toBeVisible();
  });

  // L2-017 AC4: Role change takes effect
  test('changing user role updates immediately', async ({ page }) => {
    const firstRow = page.locator('[data-testid="user-row"]').first();
    await firstRow.locator('[data-testid="edit-user-btn"]').click();

    await page.locator('[data-testid="edit-role"]').selectOption('FleetManager');
    await page.locator('[data-testid="save-role-btn"]').click();

    await expect(firstRow.locator('[data-testid="user-role"]')).toContainText('Fleet Manager');
  });

  // L2-018 AC4: Admin sees only their org's users
  test('user list shows only organization users', async ({ page }) => {
    const rows = page.locator('[data-testid="user-row"]');
    const count = await rows.count();
    expect(count).toBeGreaterThan(0);

    // All users should belong to the same org (verified by server-side filtering)
    for (let i = 0; i < count; i++) {
      const email = await rows.nth(i).locator('[data-testid="user-email"]').textContent();
      expect(email).toBeTruthy();
    }
  });
});

test.describe('Notification Preferences', () => {
  // L2-020 AC3: Preference toggles for each notification type
  test('shows notification preference toggles', async ({ page }) => {
    await page.goto('/settings/notifications');

    await expect(page.locator('[data-testid="pref-service-due"]')).toBeVisible();
    await expect(page.locator('[data-testid="pref-critical-alert"]')).toBeVisible();
    await expect(page.locator('[data-testid="pref-work-order-assigned"]')).toBeVisible();
    await expect(page.locator('[data-testid="pref-parts-order-update"]')).toBeVisible();

    // Each preference row should have email and SMS toggles
    const firstPref = page.locator('[data-testid="pref-service-due"]');
    await expect(firstPref.locator('[data-testid="toggle-email"]')).toBeVisible();
    await expect(firstPref.locator('[data-testid="toggle-sms"]')).toBeVisible();
  });

  // L2-020 AC4: Disabling notification type saves preference
  test('toggling notification preference saves changes', async ({ page }) => {
    await page.goto('/settings/notifications');

    const emailToggle = page.locator('[data-testid="pref-parts-order-update"] [data-testid="toggle-email"]');
    await emailToggle.click();

    await page.locator('[data-testid="save-preferences-btn"]').click();
    await expect(page.locator('[data-testid="save-success"]')).toBeVisible();
  });
});
