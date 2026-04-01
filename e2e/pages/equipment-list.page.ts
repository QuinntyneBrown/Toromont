import { Page, Locator } from '@playwright/test';
import { BasePage } from './base.page';

export class EquipmentListPage extends BasePage {
  readonly pageTitle: Locator;
  readonly addEquipmentButton: Locator;
  readonly statusFilter: Locator;
  readonly categoryFilter: Locator;
  readonly searchInput: Locator;
  readonly dataGrid: Locator;
  readonly gridRows: Locator;
  readonly gridHeaders: Locator;
  readonly pagination: Locator;
  readonly paginationInfo: Locator;
  readonly paginationButtons: Locator;
  readonly mobileCards: Locator;
  readonly mobileStatusChips: Locator;

  constructor(page: Page) {
    super(page);
    this.pageTitle = page.locator('[data-testid="equipment-title"]');
    this.addEquipmentButton = page.locator('[data-testid="add-equipment-btn"]');
    this.statusFilter = page.locator('[data-testid="status-filter"]');
    this.categoryFilter = page.locator('[data-testid="category-filter"]');
    this.searchInput = page.locator('[data-testid="equipment-search"]');
    this.dataGrid = page.locator('[data-testid="equipment-grid"]');
    this.gridRows = page.locator('[data-testid="equipment-row"]');
    this.gridHeaders = page.locator('[data-testid="grid-header"] th');
    this.pagination = page.locator('[data-testid="pagination"]');
    this.paginationInfo = page.locator('[data-testid="pagination-info"]');
    this.paginationButtons = page.locator('[data-testid="pagination-btn"]');
    this.mobileCards = page.locator('[data-testid="equipment-card"]');
    this.mobileStatusChips = page.locator('[data-testid="status-chip"]');
  }

  async goto() {
    await this.page.goto('/equipment');
  }

  async searchEquipment(query: string) {
    await this.searchInput.fill(query);
    await this.searchInput.press('Enter');
  }

  async selectStatusFilter(status: string) {
    await this.statusFilter.click();
    await this.page.locator(`[data-testid="status-option-${status}"]`).click();
  }

  async selectCategoryFilter(category: string) {
    await this.categoryFilter.click();
    await this.page.locator(`[data-testid="category-option-${category}"]`).click();
  }

  async clickColumnHeader(column: string) {
    await this.page.locator(`[data-testid="grid-header-${column}"]`).click();
  }

  async getRowCount(): Promise<number> {
    return this.gridRows.count();
  }

  async getRowData(index: number, column: string): Promise<string> {
    return (await this.gridRows.nth(index).locator(`[data-testid="cell-${column}"]`).textContent()) ?? '';
  }

  async clickRow(index: number) {
    await this.gridRows.nth(index).click();
  }

  async clickAddEquipment() {
    await this.addEquipmentButton.click();
  }

  async getPageNumber(): Promise<string> {
    return (await this.page.locator('[data-testid="pagination-current"]').textContent()) ?? '';
  }

  async goToPage(pageNum: number) {
    await this.page.locator(`[data-testid="pagination-page-${pageNum}"]`).click();
  }

  async selectMobileStatusChip(status: string) {
    await this.page.locator(`[data-testid="status-chip-${status.toLowerCase()}"]`).click();
  }

  async getCardName(index: number): Promise<string> {
    return (await this.mobileCards.nth(index).locator('[data-testid="card-name"]').textContent()) ?? '';
  }

  async getCardStatus(index: number): Promise<string> {
    return (await this.mobileCards.nth(index).locator('[data-testid="card-status"]').textContent()) ?? '';
  }

  async getCardSerial(index: number): Promise<string> {
    return (await this.mobileCards.nth(index).locator('[data-testid="card-serial"]').textContent()) ?? '';
  }

  async getCardHours(index: number): Promise<string> {
    return (await this.mobileCards.nth(index).locator('[data-testid="card-hours"]').textContent()) ?? '';
  }

  async getCardLocation(index: number): Promise<string> {
    return (await this.mobileCards.nth(index).locator('[data-testid="card-location"]').textContent()) ?? '';
  }
}
