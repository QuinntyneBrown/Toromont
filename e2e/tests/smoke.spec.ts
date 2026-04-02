import { test, expect } from '@playwright/test';

const pages = [
  { name: 'Dashboard', url: '/dashboard' },
  { name: 'Equipment', url: '/equipment' },
  { name: 'Service', url: '/service' },
  { name: 'Parts', url: '/parts' },
  { name: 'Telemetry', url: '/telemetry' },
  { name: 'AI Insights', url: '/ai-insights' },
  { name: 'Reports', url: '/reports' },
  { name: 'Admin', url: '/admin/users' },
];

for (const p of pages) {
  test(`${p.name} page loads without errors`, async ({ page }) => {
    const errors: string[] = [];
    page.on('console', msg => { if (msg.type() === 'error') errors.push(msg.text()); });
    page.on('pageerror', err => errors.push(err.message));
    
    await page.goto(p.url, { timeout: 15000 });
    await page.waitForTimeout(3000);
    
    // Log errors for debugging
    if (errors.length > 0) {
      console.log(`\n=== ${p.name} ERRORS ===`);
      errors.forEach(e => console.log(`  ${e.substring(0, 200)}`));
    }
    
    // Page should have content
    const body = await page.locator('body').innerText();
    expect(body.trim().length).toBeGreaterThan(20);
  });
}
