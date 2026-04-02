import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { IndicatorsModule } from '@progress/kendo-angular-indicators';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import { KpiCardComponent } from '../../shared/components/kpi-card/kpi-card.component';
import { ApiService } from '../../core/services/api.service';
import { ApiResponse, Equipment, WorkOrder, TelemetryEvent } from '../../core/models';

@Component({
  selector: 'app-equipment-detail',
  standalone: true,
  imports: [
    CommonModule, RouterModule,
    ButtonsModule, IndicatorsModule,
    BadgeComponent, KpiCardComponent
  ],
  template: `
    <div class="detail-page" *ngIf="equipment; else loadingTpl">
      <!-- Breadcrumb -->
      <div data-testid="breadcrumb" class="breadcrumb-row">
        <a routerLink="/equipment" class="bc-link">Equipment</a>
        <span class="bc-sep">/</span>
        <span class="bc-current">{{ equipment.name || (equipment.make + ' ' + equipment.model) }}</span>
      </div>

      <!-- Title Row -->
      <div class="d-flex justify-content-between align-items-center mb-4 flex-wrap gap-2">
        <div class="d-flex align-items-center gap-3">
          <h2 data-testid="equipment-name" class="fw-bold mb-0" style="font-size:22px;">{{ equipment.name || (equipment.make + ' ' + equipment.model) }}</h2>
          <span data-testid="equipment-status"><app-badge [text]="equipment.status" [variant]="getStatusVariant(equipment.status)"></app-badge></span>
        </div>
        <div class="d-flex gap-2">
          <button data-testid="schedule-service-btn" kendoButton themeColor="primary" (click)="scheduleService()">Schedule Service</button>
          <button data-testid="view-telemetry-btn" kendoButton (click)="viewTelemetry()">View Telemetry</button>
          <button data-testid="edit-equipment-btn" kendoButton (click)="goBack()">Edit</button>
        </div>
      </div>

      <!-- Two-Column Layout -->
      <div class="detail-cols">
        <!-- Left Column: Specs + Telemetry KPIs -->
        <div class="detail-left">
          <!-- Specifications Card -->
          <div data-testid="specs-panel" class="detail-card">
            <div class="card-title-row">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/></svg>
              <span class="fw-semibold" style="font-size:15px;">Specifications</span>
            </div>
            <div class="specs-grid">
              <div data-testid="spec-make" class="spec-item"><span class="spec-label">Make</span><span class="spec-value">{{ equipment.make }}</span></div>
              <div data-testid="spec-model" class="spec-item"><span class="spec-label">Model</span><span class="spec-value">{{ equipment.model }}</span></div>
              <div data-testid="spec-year" class="spec-item"><span class="spec-label">Year</span><span class="spec-value">{{ equipment.year }}</span></div>
              <div data-testid="spec-serial" class="spec-item"><span class="spec-label">Serial Number</span><span class="spec-value">{{ equipment.serialNumber }}</span></div>
              <div class="spec-item"><span class="spec-label">Category</span><span class="spec-value">{{ equipment.category }}</span></div>
              <div class="spec-item"><span class="spec-label">Status</span><span class="spec-value">{{ equipment.status }}</span></div>
            </div>
          </div>

          <!-- Telemetry KPI Cards -->
          <div data-testid="telemetry-summary" class="d-flex gap-3">
            <div data-testid="engine-hours" style="flex:1">
              <app-kpi-card label="Engine Hours" [value]="latestTelemetry?.engineHours ? (latestTelemetry.engineHours | number:'1.0-0') + ' hrs' : 'N/A'"></app-kpi-card>
            </div>
            <div data-testid="fuel-level" style="flex:1">
              <app-kpi-card label="Fuel Level" [value]="latestTelemetry?.fuelLevel ? (latestTelemetry.fuelLevel | number:'1.0-0') + '%' : 'N/A'"></app-kpi-card>
            </div>
            <div data-testid="temperature" style="flex:1">
              <app-kpi-card label="Temperature" [value]="latestTelemetry?.temperature ? (latestTelemetry.temperature | number:'1.0-0') + '°F' : 'N/A'"></app-kpi-card>
            </div>
          </div>
        </div>

        <!-- Right Column: Map + Service History -->
        <div class="detail-right">
          <!-- Location Map -->
          <div class="detail-card">
            <div class="card-title-row">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"/><circle cx="12" cy="10" r="3"/></svg>
              <span class="fw-semibold" style="font-size:14px;">{{ equipment.location || 'Unknown Location' }}</span>
            </div>
            <div data-testid="mini-map" class="map-placeholder">
              <div style="width:12px;height:12px;border-radius:50%;background:var(--status-success);"></div>
            </div>
          </div>

          <!-- Service History Timeline -->
          <div data-testid="service-history" class="detail-card">
            <div class="card-title-row">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.77-3.77a6 6 0 0 1-7.94 7.94l-6.91 6.91a2.12 2.12 0 0 1-3-3l6.91-6.91a6 6 0 0 1 7.94-7.94l-3.76 3.76z"/></svg>
              <span class="fw-semibold" style="font-size:14px;">Service History</span>
            </div>
            <div *ngIf="workOrders.length === 0" class="text-center py-4" style="color:var(--foreground-secondary);font-size:13px;">
              No service history
            </div>
            <div *ngFor="let wo of workOrders; let i = index" data-testid="service-history-item" class="timeline-item">
              <div class="timeline-dot" [ngClass]="'dot-' + wo.priority.toLowerCase()"></div>
              <div class="timeline-content">
                <div class="fw-medium" style="font-size:13px;">{{ wo.serviceType }}: {{ wo.description }}</div>
                <div style="font-size:12px;color:var(--foreground-secondary);">{{ wo.requestedDate ? (wo.requestedDate | date:'mediumDate') : (wo.createdAt | date:'mediumDate') }}</div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <ng-template #loadingTpl>
      <div class="d-flex justify-content-center align-items-center py-5">
        <kendo-loader type="infinite-spinner" size="large"></kendo-loader>
      </div>
    </ng-template>
  `,
  styles: [`
    :host { display: block; }
    .detail-page { padding: 24px; }
    .breadcrumb-row { display: flex; align-items: center; gap: 6px; margin-bottom: 12px; font-size: 13px; }
    .bc-link { color: var(--accent-primary); text-decoration: none; }
    .bc-sep { color: var(--foreground-disabled); }
    .bc-current { color: var(--foreground-secondary); }
    .detail-cols { display: flex; gap: 20px; }
    .detail-left { flex: 1; display: flex; flex-direction: column; gap: 20px; }
    .detail-right { width: 400px; display: flex; flex-direction: column; gap: 20px; flex-shrink: 0; }
    .detail-card { background: var(--surface-secondary); border: 1px solid var(--border-subtle); border-radius: var(--radius-md); padding: 20px; }
    .card-title-row { display: flex; align-items: center; gap: 8px; margin-bottom: 16px; color: var(--foreground-primary); }
    .specs-grid { display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 16px; }
    .spec-item { display: flex; flex-direction: column; gap: 4px; }
    .spec-label { font-size: 12px; color: var(--foreground-secondary); font-weight: 500; }
    .spec-value { font-size: 14px; color: var(--foreground-primary); font-weight: 500; }
    .map-placeholder { height: 180px; background: var(--surface-primary); border-radius: var(--radius-sm); display: flex; align-items: center; justify-content: center; }
    .timeline-item { display: flex; gap: 12px; padding: 12px 0; border-bottom: 1px solid var(--border-subtle); }
    .timeline-item:last-child { border-bottom: none; }
    .timeline-dot { width: 10px; height: 10px; border-radius: 50%; margin-top: 4px; flex-shrink: 0; }
    .dot-critical { background: var(--status-error); }
    .dot-high { background: var(--status-warning); }
    .dot-medium { background: var(--status-info); }
    .dot-low { background: var(--foreground-secondary); }
    @media (max-width: 992px) {
      .detail-cols { flex-direction: column; }
      .detail-right { width: 100%; }
      .specs-grid { grid-template-columns: 1fr 1fr; }
    }
  `]
})
export default class EquipmentDetailComponent implements OnInit {
  equipment: Equipment | null = null;
  workOrders: WorkOrder[] = [];
  telemetryEvents: TelemetryEvent[] = [];
  latestTelemetry: any = null;
  private equipmentId = '';

  constructor(
    private api: ApiService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.equipmentId = this.route.snapshot.paramMap.get('id') || '';
    if (this.equipmentId) {
      this.loadEquipment();
      this.loadWorkOrders();
      this.loadTelemetry();
    }
  }

  loadEquipment(): void {
    this.api.get<Equipment>(`/equipment/${this.equipmentId}`).subscribe({
      next: (res) => { this.equipment = res; },
      error: (err) => { console.error('Failed to load equipment', err); }
    });
  }

  loadWorkOrders(): void {
    this.api.get<any>('/work-orders', { skip: 0, take: 50, equipmentId: this.equipmentId }).subscribe({
      next: (res) => { this.workOrders = res.items || []; },
      error: () => { this.workOrders = []; }
    });
  }

  loadTelemetry(): void {
    this.api.get<any>(`/telemetry/${this.equipmentId}/latest`).subscribe({
      next: (res) => { this.latestTelemetry = res; },
      error: () => { this.telemetryEvents = []; }
    });
  }

  goBack(): void {
    this.router.navigate(['/equipment']);
  }

  scheduleService(): void {
    this.router.navigate(['/service'], { queryParams: { equipmentId: this.equipmentId } });
  }

  viewTelemetry(): void {
    this.router.navigate(['/telemetry', this.equipmentId]);
  }

  getStatusVariant(status: string): 'success' | 'warning' | 'error' | 'info' {
    switch (status) {
      case 'Operational': return 'success';
      case 'NeedsService': return 'warning';
      case 'OutOfService': return 'error';
      case 'Idle': return 'info';
      default: return 'info';
    }
  }

  getPriorityVariant(priority: string): 'success' | 'warning' | 'error' | 'info' {
    switch (priority) {
      case 'Critical': return 'error';
      case 'High': return 'warning';
      case 'Medium': return 'info';
      case 'Low': return 'info';
      default: return 'info';
    }
  }

  getWOStatusVariant(status: string): 'success' | 'warning' | 'error' | 'info' {
    switch (status) {
      case 'Completed': return 'success';
      case 'InProgress': return 'warning';
      case 'Open': return 'info';
      case 'Cancelled': return 'error';
      default: return 'info';
    }
  }
}
