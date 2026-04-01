import { Page, Locator } from '@playwright/test';
import { BasePage } from './base.page';

export class EquipmentDetailPage extends BasePage {
  readonly breadcrumb: Locator;
  readonly equipmentName: Locator;
  readonly statusBadge: Locator;
  readonly scheduleServiceButton: Locator;
  readonly viewTelemetryButton: Locator;
  readonly editButton: Locator;
  readonly specsPanel: Locator;
  readonly specMake: Locator;
  readonly specModel: Locator;
  readonly specYear: Locator;
  readonly specSerial: Locator;
  readonly specWeight: Locator;
  readonly specEngine: Locator;
  readonly telemetryCards: Locator;
  readonly engineHoursCard: Locator;
  readonly fuelLevelCard: Locator;
  readonly temperatureCard: Locator;
  readonly miniMap: Locator;
  readonly serviceHistory: Locator;
  readonly serviceHistoryItems: Locator;

  constructor(page: Page) {
    super(page);
    this.breadcrumb = page.locator('[data-testid="breadcrumb"]');
    this.equipmentName = page.locator('[data-testid="equipment-name"]');
    this.statusBadge = page.locator('[data-testid="equipment-status"]');
    this.scheduleServiceButton = page.locator('[data-testid="schedule-service-btn"]');
    this.viewTelemetryButton = page.locator('[data-testid="view-telemetry-btn"]');
    this.editButton = page.locator('[data-testid="edit-equipment-btn"]');
    this.specsPanel = page.locator('[data-testid="specs-panel"]');
    this.specMake = page.locator('[data-testid="spec-make"]');
    this.specModel = page.locator('[data-testid="spec-model"]');
    this.specYear = page.locator('[data-testid="spec-year"]');
    this.specSerial = page.locator('[data-testid="spec-serial"]');
    this.specWeight = page.locator('[data-testid="spec-weight"]');
    this.specEngine = page.locator('[data-testid="spec-engine"]');
    this.telemetryCards = page.locator('[data-testid="telemetry-summary"]');
    this.engineHoursCard = page.locator('[data-testid="engine-hours"]');
    this.fuelLevelCard = page.locator('[data-testid="fuel-level"]');
    this.temperatureCard = page.locator('[data-testid="temperature"]');
    this.miniMap = page.locator('[data-testid="mini-map"]');
    this.serviceHistory = page.locator('[data-testid="service-history"]');
    this.serviceHistoryItems = page.locator('[data-testid="service-history-item"]');
  }

  async goto(equipmentId: string) {
    await this.page.goto(`/equipment/${equipmentId}`);
  }

  async getServiceHistoryCount(): Promise<number> {
    return this.serviceHistoryItems.count();
  }

  async clickScheduleService() {
    await this.scheduleServiceButton.click();
  }

  async clickViewTelemetry() {
    await this.viewTelemetryButton.click();
  }
}
