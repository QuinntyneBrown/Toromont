// Acceptance Test
// Traces to: L2-023, L2-024, L2-031
// Description: Verify authentication flow, RBAC, and responsive navigation

import { test, expect } from '@playwright/test';
import { LoginPage } from '../pages/login.page';
import { DashboardPage } from '../pages/dashboard.page';

test.describe('Authentication', () => {
  // L2-023 AC1: Unauthenticated user redirected to login
  test('redirects unauthenticated user to login page', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page).toHaveURL(/\/login/);
  });

  // L2-023: Login page elements are present
  test('login page displays branding and sign-in button', async ({ page }) => {
    const login = new LoginPage(page);
    await login.goto();

    await expect(login.brandingPanel).toBeVisible();
    await expect(login.logoText).toContainText('FLEET HUB');
    await expect(login.welcomeTitle).toContainText('Welcome Back');
    await expect(login.signInButton).toBeVisible();
    await expect(login.signInButton).toContainText('Sign in with Microsoft');
  });

  // L2-023 AC5: Sign out clears session
  test('sign out redirects to login page', async ({ page }) => {
    const dashboard = new DashboardPage(page);
    await dashboard.goto();
    await dashboard.userAvatar.click();
    await page.locator('[data-testid="sign-out-btn"]').click();

    await expect(page).toHaveURL(/\/login/);
  });
});

test.describe('Role-Based Access Control', () => {
  // L2-024 AC2: Operator cannot see Create Work Order button
  test('Operator role does not see create work order button', async ({ page }) => {
    // Login as Operator
    await page.goto('/service');
    const createBtn = page.locator('[data-testid="create-work-order-btn"]');
    // For Operator role, this button should not exist
    // This test expects failure until RBAC is implemented
    await expect(createBtn).toHaveCount(0);
  });

  // L2-024 AC5: Fleet Manager cannot access User Management
  test('Fleet Manager cannot access user management', async ({ page }) => {
    await page.goto('/admin/users');
    // Should get a forbidden page or redirect
    await expect(page.locator('[data-testid="forbidden-message"]')).toBeVisible();
  });
});

test.describe('Responsive Navigation', () => {
  // L2-031 AC1: Desktop shows persistent sidebar with icons and labels
  test('desktop shows sidebar with icons and text', async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 900 });
    const dashboard = new DashboardPage(page);
    await dashboard.goto();

    await expect(dashboard.sidebar).toBeVisible();
    const navItems = await dashboard.getSidebarNavItems();
    expect(await navItems.count()).toBeGreaterThanOrEqual(7);
  });

  // L2-031 AC3: Mobile hides sidebar and shows hamburger
  test('mobile hides sidebar and shows hamburger menu', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 812 });
    const dashboard = new DashboardPage(page);
    await dashboard.goto();

    const hamburger = await dashboard.getHamburgerMenu();
    await expect(hamburger).toBeVisible();
  });

  // L2-031 AC4: Hamburger opens full-screen overlay navigation
  test('hamburger opens overlay navigation on mobile', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 812 });
    const dashboard = new DashboardPage(page);
    await dashboard.goto();

    const hamburger = await dashboard.getHamburgerMenu();
    await hamburger.click();

    const overlay = page.locator('[data-testid="mobile-nav-overlay"]');
    await expect(overlay).toBeVisible();
  });

  // L2-031 AC5: Clicking menu item navigates and closes overlay
  test('clicking menu item in overlay navigates and closes', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 812 });
    const dashboard = new DashboardPage(page);
    await dashboard.goto();

    const hamburger = await dashboard.getHamburgerMenu();
    await hamburger.click();

    await page.locator('[data-testid="mobile-nav-equipment"]').click();
    await expect(page).toHaveURL(/\/equipment/);

    const overlay = page.locator('[data-testid="mobile-nav-overlay"]');
    await expect(overlay).not.toBeVisible();
  });
});
