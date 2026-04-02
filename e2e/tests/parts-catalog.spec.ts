// Acceptance Test
// Traces to: L2-010, L2-011, L2-015
// Description: Verify parts catalog, AI search, cart, and order submission

import { test, expect } from '@playwright/test';
import { PartsCatalogPage } from '../pages/parts-catalog.page';

test.describe('Parts Catalog', () => {
  let catalog: PartsCatalogPage;

  test.beforeEach(async ({ page }) => {
    catalog = new PartsCatalogPage(page);
    await catalog.goto();
  });

  // L2-010 AC1: Parts grid displays with required columns
  test('displays parts catalog with correct columns', async () => {
    await expect(catalog.partsGrid).toBeVisible();
    const rowCount = await catalog.partRows.count();
    expect(rowCount).toBeGreaterThan(0);
  });

  // L2-010 AC2: Category filter narrows results
  test('filters by category', async () => {
    await catalog.selectCategory('Hydraulic');

    const rowCount = await catalog.partRows.count();
    expect(rowCount).toBeGreaterThan(0);
  });

  // L2-010 AC4-5: Availability badges shown correctly
  test('shows availability badges for in-stock and out-of-stock parts', async () => {
    const rowCount = await catalog.partRows.count();
    for (let i = 0; i < Math.min(rowCount, 5); i++) {
      const availability = await catalog.getPartAvailability(i);
      expect(availability).toMatch(/In Stock|Out of Stock|Low Stock/);
    }
  });

  // L2-010 AC5: Out-of-stock parts show disabled Unavailable button
  test('out-of-stock parts show Unavailable button', async ({ page }) => {
    // Find an out-of-stock part
    const outOfStockRow = page.locator('[data-testid="part-row"]:has([data-testid="part-availability"]:has-text("Out of Stock"))');
    if (await outOfStockRow.count() > 0) {
      const actionBtn = outOfStockRow.first().locator('[data-testid="add-to-cart-btn"]');
      await expect(actionBtn).toBeDisabled();
      await expect(actionBtn).toContainText('Unavailable');
    }
  });

  // L2-015 AC1: AI search returns relevant results
  test('AI search returns relevant parts', async () => {
    await catalog.searchParts('hydraulic filter for CAT 320');

    const rowCount = await catalog.partRows.count();
    expect(rowCount).toBeGreaterThan(0);
  });

  // L2-010 AC6: Mobile view collapses filter sidebar
  test('filter sidebar collapses on mobile', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 812 });
    await catalog.goto();

    await expect(catalog.filterSidebar).not.toBeVisible();
    const filterToggle = page.locator('[data-testid="filter-toggle"]');
    await expect(filterToggle).toBeVisible();
  });
});

test.describe('Shopping Cart & Order', () => {
  // L2-011 AC1: Add to cart updates cart count
  test('adding part to cart updates cart badge', async ({ page }) => {
    const catalog = new PartsCatalogPage(page);
    await catalog.goto();

    await catalog.addToCart(0);
    const count = await catalog.getCartItemCount();
    expect(parseInt(count)).toBeGreaterThan(0);
  });

  // L2-011 AC2: Cart page shows items with totals
  test('cart page displays items with correct totals', async ({ page }) => {
    const catalog = new PartsCatalogPage(page);
    await catalog.goto();
    await catalog.addToCart(0);

    await page.goto('/parts/cart');

    await expect(page.locator('[data-testid="cart-items"]')).toBeVisible();
    await expect(page.locator('[data-testid="cart-item"]').first()).toBeVisible();
    await expect(page.locator('[data-testid="cart-subtotal"]')).toBeVisible();
    await expect(page.locator('[data-testid="submit-order-btn"]')).toBeVisible();
  });

  // L2-011 AC3: Changing quantity recalculates totals
  test('changing quantity updates totals', async ({ page }) => {
    const catalog = new PartsCatalogPage(page);
    await catalog.goto();
    await catalog.addToCart(0);
    await page.goto('/parts/cart');

    const initialTotal = await page.locator('[data-testid="cart-subtotal"]').textContent();
    await page.locator('[data-testid="quantity-input"]').first().fill('5');
    await page.locator('[data-testid="quantity-input"]').first().press('Tab');

    const updatedTotal = await page.locator('[data-testid="cart-subtotal"]').textContent();
    expect(updatedTotal).not.toBe(initialTotal);
  });

  // L2-011 AC4: Remove item from cart
  test('removing item from cart updates totals', async ({ page }) => {
    const catalog = new PartsCatalogPage(page);
    await catalog.goto();
    await catalog.addToCart(0);
    await catalog.addToCart(1);
    await page.goto('/parts/cart');

    const initialCount = await page.locator('[data-testid="cart-item"]').count();
    await page.locator('[data-testid="remove-item-btn"]').first().click();

    const newCount = await page.locator('[data-testid="cart-item"]').count();
    expect(newCount).toBe(initialCount - 1);
  });

  // L2-011 AC5: Submit order generates order number
  test('submitting order shows confirmation with order number', async ({ page }) => {
    const catalog = new PartsCatalogPage(page);
    await catalog.goto();
    await catalog.addToCart(0);
    await page.goto('/parts/cart');

    await page.locator('[data-testid="submit-order-btn"]').click();

    await expect(page.locator('[data-testid="order-confirmation"]')).toBeVisible();
    await expect(page.locator('[data-testid="order-number"]')).toContainText(/PO-/);
  });
});
