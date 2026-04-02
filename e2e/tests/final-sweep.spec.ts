import { test, expect } from '@playwright/test';

const pages = [
  { name: 'Dashboard', url: '/dashboard', expect: 'Fleet Overview', dataCheck: 'TOTAL EQUIPMENT' },
  { name: 'Equipment List', url: '/equipment', expect: 'Equipment Registry', rowSelector: '[data-testid="equipment-row"]' },
  { name: 'Equipment Detail', url: '/equipment/c1b2c3d4-0001-0000-0000-000000000001', expect: 'CAT 320' },
  { name: 'Work Orders', url: '/service', expect: 'Service Management', rowSelector: 'table tbody tr' },
  { name: 'Parts Catalog', url: '/parts', expect: 'Parts Catalog', rowSelector: '[data-testid="part-row"]' },
  { name: 'Cart', url: '/parts/cart', expect: 'Shopping Cart' },
  { name: 'Telemetry', url: '/telemetry', expect: 'Telemetry' },
  { name: 'AI Insights', url: '/ai-insights', expect: 'AI Insights', rowSelector: '[data-testid="prediction-row"]' },
  { name: 'Reports', url: '/reports', expect: 'Reports' },
  { name: 'User Management', url: '/admin/users', expect: 'User Management', rowSelector: '[data-testid="user-row"]' },
];

for (const p of pages) {
  test(`${p.name} renders correctly`, async ({ page }) => {
    const errors: string[] = [];
    page.on('pageerror', err => errors.push(err.message.substring(0, 100)));
    
    await page.goto(p.url, { waitUntil: 'networkidle', timeout: 15000 });
    await page.waitForTimeout(3000);
    
    const content = await page.locator('.main-content').innerText();
    expect(content.length).toBeGreaterThan(10);
    expect(content).toContain(p.expect);
    
    if (p.rowSelector) {
      const rows = await page.locator(p.rowSelector).count();
      expect(rows).toBeGreaterThan(0);
      console.log(`${p.name}: ${rows} rows`);
    }
    
    if (p.dataCheck) {
      // Verify KPI has non-zero values
      expect(content).not.toMatch(new RegExp(p.dataCheck + '\s*\n\s*0\s*\n'));
    }
    
    expect(errors.length).toBe(0);
    console.log(`${p.name}: ✓`);
  });
}
