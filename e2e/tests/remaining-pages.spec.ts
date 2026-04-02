import { test, expect } from '@playwright/test';

test('Telemetry page loads with equipment selector', async ({ page }) => {
  const errors: string[] = [];
  page.on('pageerror', err => errors.push(err.message));
  await page.goto('/telemetry', { waitUntil: 'networkidle' });
  await page.waitForTimeout(4000);
  const content = await page.locator('.main-content').innerText();
  console.log(`Telemetry content: "${content.substring(0, 150)}"`);
  expect(content).toContain('Telemetry');
  if (errors.length) console.log('Errors:', errors.map(e => e.substring(0, 100)));
});

test('Reports page loads', async ({ page }) => {
  const errors: string[] = [];
  page.on('pageerror', err => errors.push(err.message));
  await page.goto('/reports', { waitUntil: 'networkidle' });
  await page.waitForTimeout(4000);
  const content = await page.locator('.main-content').innerText();
  console.log(`Reports content: "${content.substring(0, 150)}"`);
  expect(content).toContain('Reports');
  if (errors.length) console.log('Errors:', errors.map(e => e.substring(0, 100)));
});

test('User Management page loads with users', async ({ page }) => {
  const errors: string[] = [];
  page.on('pageerror', err => errors.push(err.message));
  await page.goto('/admin/users', { waitUntil: 'networkidle' });
  await page.waitForTimeout(4000);
  const rows = page.locator('[data-testid="user-row"]');
  const count = await rows.count();
  console.log(`User rows: ${count}`);
  expect(count).toBeGreaterThan(0);
  if (errors.length) console.log('Errors:', errors.map(e => e.substring(0, 100)));
});

test('Equipment detail page loads', async ({ page }) => {
  const errors: string[] = [];
  page.on('pageerror', err => errors.push(err.message));
  await page.goto('/equipment/c1b2c3d4-0001-0000-0000-000000000001', { waitUntil: 'networkidle' });
  await page.waitForTimeout(4000);
  const content = await page.locator('.main-content').innerText();
  console.log(`Detail content: "${content.substring(0, 200)}"`);
  expect(content).toContain('CAT');
  expect(content).toContain('Operational');
  if (errors.length) console.log('Errors:', errors.map(e => e.substring(0, 100)));
});

test('Cart page loads', async ({ page }) => {
  const errors: string[] = [];
  page.on('pageerror', err => errors.push(err.message));
  await page.goto('/parts/cart', { waitUntil: 'networkidle' });
  await page.waitForTimeout(4000);
  const content = await page.locator('.main-content').innerText();
  console.log(`Cart content: "${content.substring(0, 150)}"`);
  expect(content).toContain('Cart');
  if (errors.length) console.log('Errors:', errors.map(e => e.substring(0, 100)));
});
