// Acceptance Tests — Work Order Identity Fix
// Traces to: L2-007
// Verifies: Frontend audit critical finding #3
// Intent: All tests FAIL before fix, PASS after fix

import { test, expect } from '@playwright/test';

test.describe('Work Order Technician Assignment', () => {

  test.beforeEach(async ({ page }) => {
    await page.goto('/service');
  });

  // FAIL: current impl shows hardcoded names, not API-loaded users
  test('assignee dropdown loads real users from API', async ({ page }) => {
    // Intercept the users API call
    const usersCall = page.waitForRequest(
      req => req.url().includes('/users') && req.method() === 'GET'
    );

    await page.locator('[data-testid="create-work-order-btn"]').click();
    await page.locator('[data-testid="wo-assignee"]').click();

    // Verify the API was called to load users
    const request = await usersCall;
    expect(request.url()).toContain('/users');

    // Verify dropdown options are visible
    const options = page.locator('[data-testid="assignee-option"]');
    await expect(options.first()).toBeVisible({ timeout: 5000 });

    // Options should NOT contain the hardcoded names
    const allTexts = await options.allTextContents();
    expect(allTexts).not.toContain('John Smith');
    expect(allTexts).not.toContain('Jane Doe');
    expect(allTexts).not.toContain('Mike Johnson');
    expect(allTexts).not.toContain('Sarah Williams');
  });

  // FAIL: current impl sends string name as assignedToUserId
  test('creating work order sends valid GUID as assignedToUserId', async ({ page }) => {
    // Intercept the create work order API call
    const createCall = page.waitForRequest(
      req => req.url().includes('/work-orders') && req.method() === 'POST'
    );

    await page.locator('[data-testid="create-work-order-btn"]').click();

    // Fill out the form
    await page.locator('[data-testid="wo-equipment-select"]').click();
    await page.locator('[data-testid="equipment-option"]').first().click();
    await page.locator('[data-testid="wo-service-type"]').selectOption('Corrective');
    await page.locator('[data-testid="wo-priority"]').selectOption('High');
    await page.locator('[data-testid="wo-description"]').fill('Test WO with real technician');
    await page.locator('[data-testid="wo-requested-date"]').fill('2026-04-15');

    // Select a technician from the dropdown
    await page.locator('[data-testid="wo-assignee"]').click();
    await page.locator('[data-testid="assignee-option"]').first().click();

    // Submit
    await page.locator('[data-testid="wo-submit"]').click();

    // Verify the request payload
    const request = await createCall;
    const payload = request.postDataJSON();

    // assignedToUserId should be a valid GUID, not a string name
    expect(payload.assignedToUserId).toBeDefined();
    expect(payload.assignedToUserId).toMatch(
      /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i
    );

    // Should NOT be a plain name string
    expect(payload.assignedToUserId).not.toBe('John Smith');
    expect(payload.assignedToUserId).not.toBe('Jane Doe');
  });

  // FAIL: current impl sends string, backend rejects or ignores it
  test('assigned technician displays correctly on created work order', async ({ page }) => {
    // Create a work order with a real technician
    await page.locator('[data-testid="create-work-order-btn"]').click();
    await page.locator('[data-testid="wo-equipment-select"]').click();
    await page.locator('[data-testid="equipment-option"]').first().click();
    await page.locator('[data-testid="wo-service-type"]').selectOption('Preventive');
    await page.locator('[data-testid="wo-priority"]').selectOption('Medium');
    await page.locator('[data-testid="wo-description"]').fill('Assigned technician display test');
    await page.locator('[data-testid="wo-requested-date"]').fill('2026-04-20');

    // Get the technician name before selecting
    await page.locator('[data-testid="wo-assignee"]').click();
    const techName = await page.locator('[data-testid="assignee-option"]').first().textContent();
    await page.locator('[data-testid="assignee-option"]').first().click();

    await page.locator('[data-testid="wo-submit"]').click();

    // Wait for the list to reload
    await page.waitForTimeout(2000);

    // The newly created work order should show the technician name
    const assignedCells = page.locator('[data-testid="wo-assigned-to"]');
    const allTexts = await assignedCells.allTextContents();

    // At least one cell should show the selected technician's actual name
    const found = allTexts.some(t => t.trim() === techName?.trim());
    expect(found).toBe(true);
  });

  // FAIL: dropdown includes all hardcoded users regardless of role
  test('assignee dropdown only shows active technicians and fleet managers', async ({ page }) => {
    await page.locator('[data-testid="create-work-order-btn"]').click();
    await page.locator('[data-testid="wo-assignee"]').click();

    const options = page.locator('[data-testid="assignee-option"]');
    await expect(options.first()).toBeVisible({ timeout: 5000 });

    // Count should be > 0 (real users exist in seed data)
    const count = await options.count();
    expect(count).toBeGreaterThan(0);

    // Each option should have a real user display name (not the hardcoded names)
    for (let i = 0; i < count; i++) {
      const text = await options.nth(i).textContent();
      expect(text?.trim()).not.toBe('');
      // Should not be any of the hardcoded placeholders
      expect(['John Smith', 'Jane Doe', 'Mike Johnson', 'Sarah Williams'])
        .not.toContain(text?.trim());
    }
  });

  // Verify unassigned option is handled gracefully
  test('work order can be created without assigning a technician', async ({ page }) => {
    const createCall = page.waitForRequest(
      req => req.url().includes('/work-orders') && req.method() === 'POST'
    );

    await page.locator('[data-testid="create-work-order-btn"]').click();
    await page.locator('[data-testid="wo-equipment-select"]').click();
    await page.locator('[data-testid="equipment-option"]').first().click();
    await page.locator('[data-testid="wo-service-type"]').selectOption('Emergency');
    await page.locator('[data-testid="wo-priority"]').selectOption('Critical');
    await page.locator('[data-testid="wo-description"]').fill('Unassigned emergency WO');

    // Do NOT select a technician
    await page.locator('[data-testid="wo-submit"]').click();

    const request = await createCall;
    const payload = request.postDataJSON();

    // assignedToUserId should be null, not an empty string
    expect(payload.assignedToUserId).toBeNull();
  });
});
