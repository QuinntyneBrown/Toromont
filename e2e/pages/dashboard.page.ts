import { Page, Locator } from '@playwright/test';
import { BasePage } from './base.page';

export class DashboardPage extends BasePage {
  readonly pageTitle: Locator;
  readonly kpiCards: Locator;
  readonly kpiTotalEquipment: Locator;
  readonly kpiActiveEquipment: Locator;
  readonly kpiServiceRequired: Locator;
  readonly kpiOverdueWorkOrders: Locator;
  readonly kpiFleetUtilization: Locator;
  readonly equipmentMap: Locator;
  readonly mapMarkers: Locator;
  readonly alertsPanel: Locator;
  readonly alertsList: Locator;
  readonly alertItems: Locator;
  readonly viewAllAlertsLink: Locator;

  constructor(page: Page) {
    super(page);
    this.pageTitle = page.locator('[data-testid="dashboard-title"]');
    this.kpiCards = page.locator('[data-testid="kpi-card"]');
    this.kpiTotalEquipment = page.locator('[data-testid="kpi-total-equipment"]');
    this.kpiActiveEquipment = page.locator('[data-testid="kpi-active-equipment"]');
    this.kpiServiceRequired = page.locator('[data-testid="kpi-service-required"]');
    this.kpiOverdueWorkOrders = page.locator('[data-testid="kpi-overdue-work-orders"]');
    this.kpiFleetUtilization = page.locator('[data-testid="kpi-fleet-utilization"]');
    this.equipmentMap = page.locator('[data-testid="equipment-map"]');
    this.mapMarkers = page.locator('[data-testid="map-marker"]');
    this.alertsPanel = page.locator('[data-testid="alerts-panel"]');
    this.alertsList = page.locator('[data-testid="alerts-list"]');
    this.alertItems = page.locator('[data-testid="alert-item"]');
    this.viewAllAlertsLink = page.locator('[data-testid="view-all-alerts"]');
  }

  async goto() {
    await this.page.goto('/dashboard');
  }

  async getKpiValue(testId: string): Promise<string> {
    return (await this.page.locator(`[data-testid="${testId}"] [data-testid="kpi-value"]`).textContent()) ?? '';
  }

  async getKpiTrend(testId: string): Promise<string> {
    return (await this.page.locator(`[data-testid="${testId}"] [data-testid="kpi-trend"]`).textContent()) ?? '';
  }

  async getAlertCount(): Promise<number> {
    return this.alertItems.count();
  }

  async getAlertSeverity(index: number): Promise<string> {
    return (await this.alertItems.nth(index).locator('[data-testid="alert-severity"]').textContent()) ?? '';
  }

  async clickMapMarker(index: number) {
    await this.mapMarkers.nth(index).click();
  }

  async getMarkerPopup(): Promise<Locator> {
    return this.page.locator('[data-testid="marker-popup"]');
  }
}
