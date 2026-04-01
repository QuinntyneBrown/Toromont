import { Page, Locator } from '@playwright/test';
import { BasePage } from './base.page';

export class WorkOrdersPage extends BasePage {
  readonly pageTitle: Locator;
  readonly createWorkOrderButton: Locator;
  readonly statusTabs: Locator;
  readonly tabAll: Locator;
  readonly tabOpen: Locator;
  readonly tabInProgress: Locator;
  readonly tabOnHold: Locator;
  readonly tabCompleted: Locator;
  readonly tabClosed: Locator;
  readonly dataGrid: Locator;
  readonly gridRows: Locator;
  readonly mobileCards: Locator;

  constructor(page: Page) {
    super(page);
    this.pageTitle = page.locator('[data-testid="work-orders-title"]');
    this.createWorkOrderButton = page.locator('[data-testid="create-work-order-btn"]');
    this.statusTabs = page.locator('[data-testid="status-tabs"]');
    this.tabAll = page.locator('[data-testid="tab-all"]');
    this.tabOpen = page.locator('[data-testid="tab-open"]');
    this.tabInProgress = page.locator('[data-testid="tab-in-progress"]');
    this.tabOnHold = page.locator('[data-testid="tab-on-hold"]');
    this.tabCompleted = page.locator('[data-testid="tab-completed"]');
    this.tabClosed = page.locator('[data-testid="tab-closed"]');
    this.dataGrid = page.locator('[data-testid="work-orders-grid"]');
    this.gridRows = page.locator('[data-testid="work-order-row"]');
    this.mobileCards = page.locator('[data-testid="work-order-card"]');
  }

  async goto() {
    await this.page.goto('/service');
  }

  async clickCreateWorkOrder() {
    await this.createWorkOrderButton.click();
  }

  async selectTab(tab: string) {
    await this.page.locator(`[data-testid="tab-${tab}"]`).click();
  }

  async getRowCount(): Promise<number> {
    return this.gridRows.count();
  }

  async getRowNumber(index: number): Promise<string> {
    return (await this.gridRows.nth(index).locator('[data-testid="wo-number"]').textContent()) ?? '';
  }

  async getRowEquipment(index: number): Promise<string> {
    return (await this.gridRows.nth(index).locator('[data-testid="wo-equipment"]').textContent()) ?? '';
  }

  async getRowServiceType(index: number): Promise<string> {
    return (await this.gridRows.nth(index).locator('[data-testid="wo-service-type"]').textContent()) ?? '';
  }

  async getRowPriority(index: number): Promise<string> {
    return (await this.gridRows.nth(index).locator('[data-testid="wo-priority"]').textContent()) ?? '';
  }

  async getRowStatus(index: number): Promise<string> {
    return (await this.gridRows.nth(index).locator('[data-testid="wo-status"]').textContent()) ?? '';
  }

  async getRowAssignedTo(index: number): Promise<string> {
    return (await this.gridRows.nth(index).locator('[data-testid="wo-assigned-to"]').textContent()) ?? '';
  }

  async getRowDueDate(index: number): Promise<string> {
    return (await this.gridRows.nth(index).locator('[data-testid="wo-due-date"]').textContent()) ?? '';
  }

  async clickRow(index: number) {
    await this.gridRows.nth(index).click();
  }
}
