# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: parts-catalog.spec.ts >> Shopping Cart & Order >> submitting order shows confirmation with order number
- Location: tests\parts-catalog.spec.ts:126:7

# Error details

```
Error: expect(locator).toContainText(expected) failed

Locator: locator('[data-testid="order-number"]')
Expected pattern: /PO-/
Received string:  "ORD-20260402-002"
Timeout: 5000ms

Call log:
  - Expect "toContainText" with timeout 5000ms
  - waiting for locator('[data-testid="order-number"]')
    8 × locator resolved to <p class="fw-semibold" data-testid="order-number" _ngcontent-ng-c3327147992="">ORD-20260402-002</p>
      - unexpected value "ORD-20260402-002"

```

# Page snapshot

```yaml
- generic [ref=e3]:
  - complementary [ref=e4]:
    - generic [ref=e5]:
      - img [ref=e6]
      - generic [ref=e11]: FLEET HUB
    - navigation [ref=e12]:
      - link "Dashboard" [ref=e14] [cursor=pointer]:
        - /url: /dashboard
        - generic [ref=e16]: Dashboard
      - link "Equipment" [ref=e18] [cursor=pointer]:
        - /url: /equipment
        - generic [ref=e20]: Equipment
      - link "Service" [ref=e22] [cursor=pointer]:
        - /url: /service
        - generic [ref=e24]: Service
      - link "Parts" [ref=e26] [cursor=pointer]:
        - /url: /parts
        - generic [ref=e28]: Parts
      - link "Telemetry" [ref=e30] [cursor=pointer]:
        - /url: /telemetry
        - generic [ref=e32]: Telemetry
      - link "AI Insights" [ref=e34] [cursor=pointer]:
        - /url: /ai-insights
        - generic [ref=e36]: AI Insights
      - link "Reports" [ref=e38] [cursor=pointer]:
        - /url: /reports
        - generic [ref=e40]: Reports
      - link "Admin" [ref=e42] [cursor=pointer]:
        - /url: /admin/users
        - generic [ref=e44]: Admin
  - generic [ref=e45]:
    - banner [ref=e47]:
      - generic [ref=e49]:
        - img [ref=e50]
        - textbox "Search equipment, orders, alerts..." [ref=e53]
      - button [ref=e56] [cursor=pointer]:
        - img [ref=e57]
    - main [ref=e60]:
      - generic [ref=e62]:
        - generic [ref=e63]:
          - heading "Shopping Cart" [level=2] [ref=e64]
          - link "← Continue Shopping" [ref=e65] [cursor=pointer]:
            - /url: /parts
        - generic [ref=e67]:
          - generic [ref=e68]: 🛒
          - heading "Your cart is empty" [level=5] [ref=e69]
          - paragraph [ref=e70]: Browse the parts catalog to add items to your cart.
          - link "Browse Parts" [ref=e71] [cursor=pointer]:
            - /url: /parts
        - generic [ref=e73]:
          - generic [ref=e74]:
            - heading "Order Submitted" [level=5] [ref=e75]
            - button [ref=e76] [cursor=pointer]
          - generic [ref=e77]:
            - generic [ref=e78]: ✓
            - heading "Order Submitted Successfully" [level=5] [ref=e79]
            - paragraph [ref=e80]: Your order has been placed and is being processed.
            - paragraph [ref=e81]: ORD-20260402-002
          - button "OK" [ref=e83] [cursor=pointer]
```

# Test source

```ts
  35  |       const availability = await catalog.getPartAvailability(i);
  36  |       expect(availability).toMatch(/In Stock|Out of Stock|Low Stock/);
  37  |     }
  38  |   });
  39  | 
  40  |   // L2-010 AC5: Out-of-stock parts show disabled Unavailable button
  41  |   test('out-of-stock parts show Unavailable button', async ({ page }) => {
  42  |     // Find an out-of-stock part
  43  |     const outOfStockRow = page.locator('[data-testid="part-row"]:has([data-testid="part-availability"]:has-text("Out of Stock"))');
  44  |     if (await outOfStockRow.count() > 0) {
  45  |       const actionBtn = outOfStockRow.first().locator('[data-testid="add-to-cart-btn"]');
  46  |       await expect(actionBtn).toBeDisabled();
  47  |       await expect(actionBtn).toContainText('Unavailable');
  48  |     }
  49  |   });
  50  | 
  51  |   // L2-015 AC1: AI search returns relevant results
  52  |   test('AI search returns relevant parts', async () => {
  53  |     await catalog.searchParts('hydraulic filter for CAT 320');
  54  | 
  55  |     const rowCount = await catalog.partRows.count();
  56  |     expect(rowCount).toBeGreaterThan(0);
  57  |   });
  58  | 
  59  |   // L2-010 AC6: Mobile view collapses filter sidebar
  60  |   test('filter sidebar collapses on mobile', async ({ page }) => {
  61  |     await page.setViewportSize({ width: 375, height: 812 });
  62  |     await catalog.goto();
  63  | 
  64  |     await expect(catalog.filterSidebar).not.toBeVisible();
  65  |     const filterToggle = page.locator('[data-testid="filter-toggle"]');
  66  |     await expect(filterToggle).toBeVisible();
  67  |   });
  68  | });
  69  | 
  70  | test.describe('Shopping Cart & Order', () => {
  71  |   // L2-011 AC1: Add to cart updates cart count
  72  |   test('adding part to cart updates cart badge', async ({ page }) => {
  73  |     const catalog = new PartsCatalogPage(page);
  74  |     await catalog.goto();
  75  | 
  76  |     await catalog.addToCart(0);
  77  |     const count = await catalog.getCartItemCount();
  78  |     expect(parseInt(count)).toBeGreaterThan(0);
  79  |   });
  80  | 
  81  |   // L2-011 AC2: Cart page shows items with totals
  82  |   test('cart page displays items with correct totals', async ({ page }) => {
  83  |     const catalog = new PartsCatalogPage(page);
  84  |     await catalog.goto();
  85  |     await catalog.addToCart(0);
  86  | 
  87  |     await page.goto('/parts/cart');
  88  | 
  89  |     await expect(page.locator('[data-testid="cart-items"]')).toBeVisible();
  90  |     await expect(page.locator('[data-testid="cart-item"]').first()).toBeVisible();
  91  |     await expect(page.locator('[data-testid="cart-subtotal"]')).toBeVisible();
  92  |     await expect(page.locator('[data-testid="submit-order-btn"]')).toBeVisible();
  93  |   });
  94  | 
  95  |   // L2-011 AC3: Changing quantity recalculates totals
  96  |   test('changing quantity updates totals', async ({ page }) => {
  97  |     const catalog = new PartsCatalogPage(page);
  98  |     await catalog.goto();
  99  |     await catalog.addToCart(0);
  100 |     await page.goto('/parts/cart');
  101 | 
  102 |     const initialTotal = await page.locator('[data-testid="cart-subtotal"]').textContent();
  103 |     await page.locator('[data-testid="quantity-input"]').first().fill('5');
  104 |     await page.locator('[data-testid="quantity-input"]').first().press('Tab');
  105 | 
  106 |     const updatedTotal = await page.locator('[data-testid="cart-subtotal"]').textContent();
  107 |     expect(updatedTotal).not.toBe(initialTotal);
  108 |   });
  109 | 
  110 |   // L2-011 AC4: Remove item from cart
  111 |   test('removing item from cart updates totals', async ({ page }) => {
  112 |     const catalog = new PartsCatalogPage(page);
  113 |     await catalog.goto();
  114 |     await catalog.addToCart(0);
  115 |     await catalog.addToCart(1);
  116 |     await page.goto('/parts/cart');
  117 | 
  118 |     const initialCount = await page.locator('[data-testid="cart-item"]').count();
  119 |     await page.locator('[data-testid="remove-item-btn"]').first().click();
  120 | 
  121 |     const newCount = await page.locator('[data-testid="cart-item"]').count();
  122 |     expect(newCount).toBe(initialCount - 1);
  123 |   });
  124 | 
  125 |   // L2-011 AC5: Submit order generates order number
  126 |   test('submitting order shows confirmation with order number', async ({ page }) => {
  127 |     const catalog = new PartsCatalogPage(page);
  128 |     await catalog.goto();
  129 |     await catalog.addToCart(0);
  130 |     await page.goto('/parts/cart');
  131 | 
  132 |     await page.locator('[data-testid="submit-order-btn"]').click();
  133 | 
  134 |     await expect(page.locator('[data-testid="order-confirmation"]')).toBeVisible();
> 135 |     await expect(page.locator('[data-testid="order-number"]')).toContainText(/PO-/);
      |                                                                ^ Error: expect(locator).toContainText(expected) failed
  136 |   });
  137 | });
  138 | 
```