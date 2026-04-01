import { Page, Locator } from '@playwright/test';

export class LoginPage {
  readonly page: Page;
  readonly brandingPanel: Locator;
  readonly logoText: Locator;
  readonly tagline: Locator;
  readonly welcomeTitle: Locator;
  readonly subtitle: Locator;
  readonly signInButton: Locator;
  readonly entraIdBadge: Locator;
  readonly copyrightText: Locator;

  constructor(page: Page) {
    this.page = page;
    this.brandingPanel = page.locator('[data-testid="login-branding"]');
    this.logoText = page.locator('[data-testid="logo-text"]');
    this.tagline = page.locator('[data-testid="login-tagline"]');
    this.welcomeTitle = page.locator('[data-testid="welcome-title"]');
    this.subtitle = page.locator('[data-testid="login-subtitle"]');
    this.signInButton = page.locator('[data-testid="sign-in-microsoft"]');
    this.entraIdBadge = page.locator('[data-testid="entra-id-badge"]');
    this.copyrightText = page.locator('[data-testid="copyright"]');
  }

  async goto() {
    await this.page.goto('/login');
  }

  async clickSignIn() {
    await this.signInButton.click();
  }

  async isLoaded(): Promise<boolean> {
    await this.signInButton.waitFor({ state: 'visible' });
    return true;
  }
}
