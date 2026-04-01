import { Page, Locator } from '@playwright/test';
import { BasePage } from './base.page';

export class PartsCatalogPage extends BasePage {
  readonly aiSearchBar: Locator;
  readonly filterSidebar: Locator;
  readonly categoryFilters: Locator;
  readonly compatibilityFilter: Locator;
  readonly partsGrid: Locator;
  readonly partRows: Locator;
  readonly addToCartButtons: Locator;
  readonly cartIcon: Locator;
  readonly cartBadge: Locator;

  constructor(page: Page) {
    super(page);
    this.aiSearchBar = page.locator('[data-testid="ai-search"]');
    this.filterSidebar = page.locator('[data-testid="filter-sidebar"]');
    this.categoryFilters = page.locator('[data-testid="category-filter-item"]');
    this.compatibilityFilter = page.locator('[data-testid="compatibility-filter"]');
    this.partsGrid = page.locator('[data-testid="parts-grid"]');
    this.partRows = page.locator('[data-testid="part-row"]');
    this.addToCartButtons = page.locator('[data-testid="add-to-cart-btn"]');
    this.cartIcon = page.locator('[data-testid="cart-icon"]');
    this.cartBadge = page.locator('[data-testid="cart-badge"]');
  }

  async goto() {
    await this.page.goto('/parts');
  }

  async searchParts(query: string) {
    await this.aiSearchBar.fill(query);
    await this.aiSearchBar.press('Enter');
  }

  async selectCategory(category: string) {
    await this.page.locator(`[data-testid="category-${category}"]`).click();
  }

  async addToCart(index: number) {
    await this.addToCartButtons.nth(index).click();
  }

  async getPartAvailability(index: number): Promise<string> {
    return (await this.partRows.nth(index).locator('[data-testid="part-availability"]').textContent()) ?? '';
  }

  async getPartPrice(index: number): Promise<string> {
    return (await this.partRows.nth(index).locator('[data-testid="part-price"]').textContent()) ?? '';
  }

  async isAddToCartEnabled(index: number): Promise<boolean> {
    return this.addToCartButtons.nth(index).isEnabled();
  }

  async getCartItemCount(): Promise<string> {
    return (await this.cartBadge.textContent()) ?? '0';
  }
}
