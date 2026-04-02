import { test } from '@playwright/test';

test('Deep debug: intercept response and check DOM updates', async ({ page }) => {
  // Intercept the KPI API call and log the response
  await page.route('**/api/v1/dashboard/kpis', async route => {
    const response = await route.fetch();
    const body = await response.json();
    console.log(`\n[INTERCEPTED] KPI response: ${JSON.stringify(body)}`);
    await route.fulfill({ response, json: body });
  });
  
  await page.goto('/dashboard', { waitUntil: 'load' });
  await page.waitForTimeout(5000);
  
  // Use page.evaluate to check the component state directly
  const componentState = await page.evaluate(() => {
    // Check if Angular app is bootstrapped
    const appRef = (window as any).ng;
    const mainContent = document.querySelector('.main-content');
    const kpiCards = document.querySelectorAll('app-kpi-card');
    
    return {
      hasNg: !!appRef,
      mainContentText: mainContent?.textContent?.substring(0, 200) || 'NOT FOUND',
      kpiCardCount: kpiCards.length,
      kpiTexts: Array.from(kpiCards).map(c => c.textContent?.trim().substring(0, 50))
    };
  });
  
  console.log('\n=== Component State ===');
  console.log(JSON.stringify(componentState, null, 2));
});
