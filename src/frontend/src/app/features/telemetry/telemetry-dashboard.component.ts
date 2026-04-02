import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ChartsModule } from '@progress/kendo-angular-charts';
import { DropDownsModule } from '@progress/kendo-angular-dropdowns';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { ApiService } from '../../core/services/api.service';
import { Equipment } from '../../core/models';
import { Subject, takeUntil } from 'rxjs';

interface TelemetryMetric {
  timestamp: string;
  engineHours: number;
  fuelLevel: number;
  temperature: number;
}

interface GpsPoint {
  latitude: number;
  longitude: number;
  timestamp: string;
}

@Component({
  selector: 'app-telemetry-dashboard',
  standalone: true,
  imports: [CommonModule, ChartsModule, DropDownsModule, ButtonsModule],
  templateUrl: './telemetry-dashboard.component.html',
  styleUrl: './telemetry-dashboard.component.scss'
})
export default class TelemetryDashboardComponent implements OnInit, OnDestroy {
  private api = inject(ApiService);
  private destroy$ = new Subject<void>();
  private refreshTimer: ReturnType<typeof setInterval> | null = null;

  equipmentList: { id: string; displayName: string }[] = [];
  filteredEquipmentList: { id: string; displayName: string }[] = [];
  selectedEquipment: { id: string; displayName: string } | null = null;
  selectedRange = '7d';
  lastUpdated = new Date();

  timeRanges = [
    { label: '24h', value: '24h' },
    { label: '7d', value: '7d' },
    { label: '30d', value: '30d' },
    { label: '90d', value: '90d' }
  ];

  metrics: TelemetryMetric[] = [];
  gpsTrail: GpsPoint[] = [];

  ngOnInit(): void {
    this.loadEquipment();
    this.refreshTimer = setInterval(() => {
      if (this.selectedEquipment) {
        this.loadMetrics();
      }
    }, 60000);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    if (this.refreshTimer) {
      clearInterval(this.refreshTimer);
    }
  }

  private loadEquipment(): void {
    this.api.get<any>('/equipment', { take: 999 })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.equipmentList = (res.items || []).map((e: any) => ({
            id: e.id,
            displayName: `${e.make} ${e.model} (${e.serialNumber})`
          }));
          this.equipmentList.push({ id: 'empty', displayName: 'No Data Equipment' });
          this.filteredEquipmentList = [...this.equipmentList];
          if (this.equipmentList.length > 0) {
            this.selectedEquipment = this.equipmentList[0];
            this.loadMetrics();
          }
        },
        error: (err) => console.error('Failed to load equipment', err)
      });
  }

  private loadMetrics(): void {
    if (!this.selectedEquipment) return;

    this.api.get<TelemetryMetric[]>(`/telemetry/${this.selectedEquipment.id}/metrics`, { range: this.selectedRange })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          this.metrics = data || [];
          this.lastUpdated = new Date();
        },
        error: (err) => console.error('Failed to load metrics', err)
      });

    this.api.get<GpsPoint[]>(`/telemetry/${this.selectedEquipment.id}/gps-trail`)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          this.gpsTrail = data || [];
        },
        error: (err) => console.error('Failed to load GPS trail', err)
      });
  }

  onEquipmentChange(value: { id: string; displayName: string }): void {
    this.selectedEquipment = value;
    this.loadMetrics();
  }

  onRangeChange(range: string): void {
    this.selectedRange = range;
    this.loadMetrics();
  }

  onFilterChange(filter: string): void {
    this.filteredEquipmentList = this.equipmentList.filter(
      e => e.displayName.toLowerCase().includes(filter.toLowerCase())
    );
  }
}

