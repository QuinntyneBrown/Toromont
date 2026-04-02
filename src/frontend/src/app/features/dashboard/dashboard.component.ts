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
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
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

  mapMarkers = [
    { x: 25, y: 30, label: 'CAT 320 Excavator' },
    { x: 55, y: 50, label: 'CAT D6 Dozer' },
    { x: 70, y: 25, label: 'CAT 745 Truck' },
    { x: 40, y: 70, label: 'CAT 950 Loader' },
    { x: 80, y: 60, label: 'CAT 140 Grader' }
  ];
  selectedMarker: number | null = null;

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

