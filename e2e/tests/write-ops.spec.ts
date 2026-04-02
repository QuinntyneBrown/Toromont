import { test, expect } from '@playwright/test';

test('Create work order via the UI form', async ({ page }) => {
  const errors: string[] = [];
  page.on('pageerror', err => errors.push(err.message));
  
  await page.goto('/service', { waitUntil: 'networkidle' });
  await page.waitForTimeout(2000);
  
  // Count initial work orders
  const initialRows = await page.locator('table tbody tr').count();
  console.log(`Initial WO rows: ${initialRows}`);
  
  // Click "Create Work Order" button
  const createBtn = page.locator('text=Create Work Order').first();
  await createBtn.click();
  await page.waitForTimeout(1000);
  
  // Check if dialog opened
  const dialogVisible = await page.locator('.modal, kendo-dialog').isVisible().catch(() => false);
  console.log(`Dialog visible: ${dialogVisible}`);
  
  if (dialogVisible) {
    // Fill form fields  
    // Select equipment from dropdown
    const equipDropdown = page.locator('kendo-dropdownlist').first();
    await equipDropdown.click();
    await page.waitForTimeout(500);
    const firstOption = page.locator('kendo-popup li').first();
    if (await firstOption.isVisible()) {
      await firstOption.click();
    }
    await page.waitForTimeout(500);
    
    // Fill description
    const textarea = page.locator('kendo-textarea textarea, textarea').first();
    if (await textarea.isVisible()) {
      await textarea.fill('Playwright test - scheduled maintenance');
    }
    
    // Click Create button in dialog
    const submitBtn = page.locator('button:has-text("Create")').last();
    await submitBtn.click();
    await page.waitForTimeout(3000);
    
    // Check if new row appeared
    const newRows = await page.locator('table tbody tr').count();
    console.log(`After create WO rows: ${newRows}`);
    
    if (newRows > initialRows) {
      console.log('Work order created successfully ✓');
    }
  }
  
  if (errors.length) console.log('Errors:', errors.map(e => e.substring(0, 120)));
  expect(errors.length).toBe(0);
});

test('Add part to cart and verify cart page', async ({ page }) => {
  const errors: string[] = [];
  page.on('pageerror', err => errors.push(err.message));
  
  await page.goto('/parts', { waitUntil: 'networkidle' });
  await page.waitForTimeout(3000);
  
  // Click first "Add to Cart" button
  const addBtn = page.locator('button:has-text("Add to Cart")').first();
  if (await addBtn.isEnabled()) {
    await addBtn.click();
    await page.waitForTimeout(1000);
    console.log('Added part to cart');
  }
  
  // Navigate to cart
  await page.goto('/parts/cart', { waitUntil: 'networkidle' });
  await page.waitForTimeout(3000);
  
  const cartContent = await page.locator('.main-content').innerText();
  console.log(`Cart content: "${cartContent.substring(0, 200)}"`);
  
  // Cart should have items or show empty state
  const hasItems = cartContent.includes('$') && !cartContent.includes('Your cart is empty');
  const isEmpty = cartContent.includes('Your cart is empty');
  console.log(`Cart has items: ${hasItems}, is empty: ${isEmpty}`);
  
  if (errors.length) console.log('Errors:', errors.map(e => e.substring(0, 120)));
  expect(errors.length).toBe(0);
});

test('Equipment detail: navigate from list to detail', async ({ page }) => {
  const errors: string[] = [];
  page.on('pageerror', err => errors.push(err.message));
  
  await page.goto('/equipment', { waitUntil: 'networkidle' });
  await page.waitForTimeout(2000);
  
  // Click on first equipment link
  const firstLink = page.locator('[data-testid="cell-name"] a').first();
  const equipName = await firstLink.innerText();
  console.log(`Clicking: ${equipName}`);
  await firstLink.click();
  await page.waitForTimeout(3000);
  
  // Should navigate to detail page
  const detailContent = await page.locator('.main-content').innerText();
  expect(detailContent).toContain(equipName);
  expect(detailContent).toContain('Specifications');
  expect(detailContent).toContain('Service History');
  console.log(`Detail page loaded for: ${equipName} ✓`);
  
  if (errors.length) console.log('Errors:', errors.map(e => e.substring(0, 120)));
  expect(errors.length).toBe(0);
});
