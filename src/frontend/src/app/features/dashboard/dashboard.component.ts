import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { KpiCardComponent } from '../../shared/components/kpi-card/kpi-card.component';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import { ApiService } from '../../core/services/api.service';
import { Alert } from '../../core/models';
import { Subject, takeUntil } from 'rxjs';

interface DashboardKpis {
  totalEquipment: number;
  activeEquipment: number;
  serviceRequired: number;
  overdueWorkOrders: number;
  fleetUtilization: number;
  totalEquipmentTrend?: string;
  activeEquipmentTrend?: string;
  serviceRequiredTrend?: string;
  overdueWorkOrdersTrend?: string;
  fleetUtilizationTrend?: string;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, KpiCardComponent, BadgeComponent],
  template: `
    <div class="container-fluid p-4">
      <h2 class="mb-0 fw-bold" style="font-size: 22px; margin-bottom: 20px !important;">Fleet Overview</h2>

      <!-- KPI Cards Row -->
      <div class="row g-3 mb-4">
        <div class="col-12 col-sm-6 col-lg">
          <app-kpi-card label="Total Equipment" [value]="kpis.totalEquipment" [trendValue]="kpis.totalEquipmentTrend" [trendDirection]="'stable'"></app-kpi-card>
        </div>
        <div class="col-12 col-sm-6 col-lg">
          <app-kpi-card label="Active" [value]="kpis.activeEquipment" [trendValue]="kpis.activeEquipmentTrend" [trendDirection]="'up'"></app-kpi-card>
        </div>
        <div class="col-12 col-sm-6 col-lg">
          <app-kpi-card label="Service Required" [value]="kpis.serviceRequired" [trendValue]="kpis.serviceRequiredTrend" [trendDirection]="kpis.serviceRequired > 0 ? 'down' : 'stable'"></app-kpi-card>
        </div>
        <div class="col-12 col-sm-6 col-lg">
          <app-kpi-card label="Overdue Work Orders" [value]="kpis.overdueWorkOrders" [trendValue]="kpis.overdueWorkOrdersTrend" [trendDirection]="kpis.overdueWorkOrders > 0 ? 'down' : 'stable'"></app-kpi-card>
        </div>
        <div class="col-12 col-sm-6 col-lg">
          <app-kpi-card label="Fleet Utilization" [value]="kpis.fleetUtilization + '%'" [trendValue]="kpis.fleetUtilizationTrend" [trendDirection]="'up'"></app-kpi-card>
        </div>
      </div>

      <!-- Two-column: Map + Alerts -->
      <div class="row g-4">
        <!-- Equipment Locations Map -->
        <div class="col-12 col-lg-7">
          <div class="card h-100">
            <div class="card-header d-flex justify-content-between align-items-center bg-white">
              <h5 class="mb-0 fw-semibold" style="font-size: 15px;">Equipment Locations</h5>
              <span class="text-muted" style="font-size: 12px;">{{ kpis.totalEquipment }} units tracked</span>
            </div>
            <div class="card-body d-flex align-items-center justify-content-center" style="min-height: 300px; background: var(--surface-primary);">
              <span class="text-muted">Map view — Leaflet integration</span>
            </div>
          </div>
        </div>

        <!-- Active Alerts Panel -->
        <div class="col-12 col-lg-5">
          <div class="card h-100">
            <div class="card-header d-flex justify-content-between align-items-center bg-white">
              <h5 class="mb-0 fw-semibold" style="font-size: 15px;">Active Alerts</h5>
              <a routerLink="/alerts" class="text-decoration-none" style="font-size: 13px; color: var(--status-info);">View All</a>
            </div>
            <div class="card-body p-0">
              <div *ngIf="alerts.length === 0" class="text-center text-muted py-4">
                No active alerts
              </div>
              <div *ngFor="let alert of alerts" class="alert-row d-flex align-items-center px-3 py-3" style="border-bottom: 1px solid var(--border-subtle);">
                <span class="severity-dot me-3" [ngClass]="'dot-' + alert.severity.toLowerCase()"></span>
                <div class="flex-grow-1">
                  <div class="fw-medium" style="font-size: 13px;">{{ alert.equipmentName || 'Equipment' }} - {{ alert.message }}</div>
                  <div style="font-size: 12px; color: var(--foreground-secondary);">{{ getTimeAgo(alert.createdAt) }}</div>
                </div>
                <app-badge [text]="alert.severity" [variant]="getSeverityVariant(alert.severity)"></app-badge>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .severity-dot {
      width: 10px;
      height: 10px;
      border-radius: 50%;
      flex-shrink: 0;
    }
    .dot-critical { background-color: var(--status-error); }
    .dot-high { background-color: var(--status-warning); }
    .dot-medium { background-color: var(--status-warning); }
    .dot-low { background-color: var(--status-info); }

    .alert-row:last-child {
      border-bottom: none !important;
    }
    .alert-row:hover {
      background-color: var(--surface-primary);
    }

    .card {
      border: 1px solid var(--border-subtle);
      border-radius: var(--radius-md);
      overflow: hidden;
      background: var(--surface-secondary);
    }
    .card-header {
      border-bottom: 1px solid var(--border-subtle);
      padding: 14px 16px;
    }
  `]
})
export default class DashboardComponent implements OnInit, OnDestroy {
  private api = inject(ApiService);
  private destroy$ = new Subject<void>();
  private refreshTimer: ReturnType<typeof setInterval> | null = null;

  kpis: DashboardKpis = {
    totalEquipment: 0,
    activeEquipment: 0,
    serviceRequired: 0,
    overdueWorkOrders: 0,
    fleetUtilization: 0
  };

  alerts: (Alert & { equipmentName?: string })[] = [];
  lastUpdated = new Date();

  ngOnInit(): void {
    this.loadData();
    this.refreshTimer = setInterval(() => this.loadData(), 60000);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    if (this.refreshTimer) {
      clearInterval(this.refreshTimer);
    }
  }

  private loadData(): void {
    this.api.get<DashboardKpis>('/dashboard/kpis')
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          this.kpis = data;
          this.lastUpdated = new Date();
        },
        error: (err) => console.error('Failed to load KPIs', err)
      });

    this.api.get<(Alert & { equipmentName?: string })[]>('/dashboard/alerts')
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          this.alerts = data;
        },
        error: (err) => console.error('Failed to load alerts', err)
      });
  }

  getSeverityVariant(severity: string): 'success' | 'warning' | 'error' | 'info' {
    switch (severity.toLowerCase()) {
      case 'critical': return 'error';
      case 'high': return 'warning';
      case 'medium': return 'warning';
      case 'low': return 'info';
      default: return 'info';
    }
  }

  getTimeAgo(dateStr: string): string {
    const diff = Date.now() - new Date(dateStr).getTime();
    const minutes = Math.floor(diff / 60000);
    if (minutes < 1) return 'Just now';
    if (minutes < 60) return `${minutes}m ago`;
    const hours = Math.floor(minutes / 60);
    if (hours < 24) return `${hours}h ago`;
    const days = Math.floor(hours / 24);
    return `${days}d ago`;
  }
}
