import { test } from '@playwright/test';

test('Capture API response bodies', async ({ page }) => {
  page.on('response', async res => {
    if (res.url().includes('/api/v1/dashboard/kpis')) {
      const body = await res.text();
      console.log(`\nKPIs response body: ${body.substring(0, 200)}`);
    }
    if (res.url().includes('/api/v1/dashboard/alerts')) {
      const body = await res.text();
      console.log(`\nAlerts response body: ${body.substring(0, 200)}`);
    }
    if (res.url().includes('/api/v1/notifications/unread-count')) {
      const body = await res.text();
      console.log(`\nUnread count response: ${body}`);
    }
  });
  
  await page.goto('/dashboard', { waitUntil: 'load' });
  await page.waitForTimeout(8000);
  
  // Check what the page actually shows
  const bodyText = await page.locator('.main-content').innerText();
  console.log(`\nPage content: "${bodyText.substring(0, 300)}"`);
});
