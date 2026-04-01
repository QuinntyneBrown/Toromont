import { Page, Locator } from '@playwright/test';

export class BasePage {
  readonly page: Page;
  readonly sidebar: Locator;
  readonly headerSearch: Locator;
  readonly notificationBell: Locator;
  readonly notificationBadge: Locator;
  readonly userAvatar: Locator;
  readonly mobileNavOverlay: Locator;
  readonly bottomNav: Locator;
  readonly bottomNavItems: Locator;

  constructor(page: Page) {
    this.page = page;
    this.sidebar = page.locator('[data-testid="sidebar"]');
    this.headerSearch = page.locator('[data-testid="header-search"]');
    this.notificationBell = page.locator('[data-testid="notification-bell"]');
    this.notificationBadge = page.locator('[data-testid="notification-badge"]');
    this.userAvatar = page.locator('[data-testid="user-avatar"]');
    this.mobileNavOverlay = page.locator('[data-testid="mobile-nav-overlay"]');
    this.bottomNav = page.locator('[data-testid="bottom-nav"]');
    this.bottomNavItems = page.locator('[data-testid="bottom-nav-item"]');
  }

  async navigateTo(path: string) {
    await this.page.goto(path);
  }

  async getSidebarNavItems() {
    return this.sidebar.locator('[data-testid="nav-item"]');
  }

  async clickNavItem(name: string) {
    await this.sidebar.locator(`[data-testid="nav-item-${name.toLowerCase()}"]`).click();
  }

  async getNotificationCount(): Promise<string> {
    return (await this.notificationBadge.textContent()) ?? '0';
  }

  async isSidebarVisible(): Promise<boolean> {
    return this.sidebar.isVisible();
  }

  async getHamburgerMenu(): Promise<Locator> {
    return this.page.locator('[data-testid="hamburger-menu"]');
  }

  async clickBottomNavItem(name: string) {
    await this.page.locator(`[data-testid="bottom-nav-${name.toLowerCase()}"]`).click();
  }
}
