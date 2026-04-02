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
  template: `
    <div class="container-fluid p-4">
      <!-- Header -->
      <div class="d-flex justify-content-between align-items-center mb-4">
        <h2 class="mb-0 fw-bold">Telemetry &amp; Health</h2>
        <span class="text-muted" style="font-size: 13px;">Last updated: {{ lastUpdated | date:'medium' }} <span data-testid="auto-refresh-indicator">(auto-refresh every 60s)</span></span>
      </div>

      <!-- Controls Row -->
      <div class="d-flex flex-wrap gap-3 align-items-center mb-4">
        <div style="min-width: 260px;">
          <kendo-dropdownlist
            data-testid="equipment-selector"
            [data]="equipmentList"
            [textField]="'displayName'"
            [valueField]="'id'"
            [value]="selectedEquipment"
            [valuePrimitive]="false"
            (valueChange)="onEquipmentChange($event)"
            [filterable]="true"
            (filterChange)="onFilterChange($event)"
            placeholder="Select Equipment">
            <ng-template kendoDropDownListItemTemplate let-dataItem>
              <span [attr.data-testid]="dataItem.id === 'empty' ? 'equipment-option-empty' : null">{{ dataItem.displayName }}</span>
            </ng-template>
          </kendo-dropdownlist>
        </div>
        <div class="btn-group">
          <button *ngFor="let range of timeRanges"
                  class="btn btn-sm"
                  [class.btn-primary]="selectedRange === range.value"
                  [class.btn-outline-secondary]="selectedRange !== range.value"
                  [class.active]="selectedRange === range.value"
                  [attr.data-testid]="'time-range-' + range.value"
                  (click)="onRangeChange(range.value)">
            {{ range.label }}
          </button>
        </div>
      </div>

      <!-- Charts 2x2 Grid -->
      <div class="row g-4">
        <!-- Engine Hours -->
        <div class="col-12 col-lg-6">
          <div class="chart-card" data-testid="chart-engine-hours">
            <div class="chart-header">
              <h6 class="mb-0 fw-semibold">Engine Hours</h6>
            </div>
            <div class="chart-body">
              <kendo-chart [style.height.px]="280" *ngIf="metrics.length > 0">
                <kendo-chart-series>
                  <kendo-chart-series-item
                    type="line"
                    [data]="metrics"
                    field="engineHours"
                    categoryField="timestamp"
                    [color]="'#FFCD11'"
                    [markers]="{ visible: true, size: 4 }">
                  </kendo-chart-series-item>
                </kendo-chart-series>
                <kendo-chart-category-axis>
                  <kendo-chart-category-axis-item [labels]="{ rotation: -45, font: '11px sans-serif' }">
                  </kendo-chart-category-axis-item>
                </kendo-chart-category-axis>
                <kendo-chart-value-axis>
                  <kendo-chart-value-axis-item [title]="{ text: 'Hours' }">
                  </kendo-chart-value-axis-item>
                </kendo-chart-value-axis>
                <kendo-chart-tooltip [shared]="true"></kendo-chart-tooltip>
              </kendo-chart>
              <div *ngIf="metrics.length === 0" class="text-center text-muted py-5" data-testid="no-data-message">
                Select equipment to view telemetry data
              </div>
            </div>
          </div>
        </div>

        <!-- Fuel Consumption -->
        <div class="col-12 col-lg-6">
          <div class="chart-card" data-testid="chart-fuel-consumption">
            <div class="chart-header">
              <h6 class="mb-0 fw-semibold">Fuel Consumption</h6>
            </div>
            <div class="chart-body">
              <kendo-chart [style.height.px]="280" *ngIf="metrics.length > 0">
                <kendo-chart-series>
                  <kendo-chart-series-item
                    type="column"
                    [data]="metrics"
                    field="fuelLevel"
                    categoryField="timestamp"
                    [color]="'#FFCD11'">
                  </kendo-chart-series-item>
                </kendo-chart-series>
                <kendo-chart-category-axis>
                  <kendo-chart-category-axis-item [labels]="{ rotation: -45, font: '11px sans-serif' }">
                  </kendo-chart-category-axis-item>
                </kendo-chart-category-axis>
                <kendo-chart-value-axis>
                  <kendo-chart-value-axis-item [title]="{ text: 'Liters' }">
                  </kendo-chart-value-axis-item>
                </kendo-chart-value-axis>
                <kendo-chart-tooltip [shared]="true"></kendo-chart-tooltip>
              </kendo-chart>
              <div *ngIf="metrics.length === 0" class="text-center text-muted py-5" data-testid="no-data-message">
                Select equipment to view telemetry data
              </div>
            </div>
          </div>
        </div>

        <!-- Temperature -->
        <div class="col-12 col-lg-6">
          <div class="chart-card" data-testid="chart-temperature">
            <div class="chart-header">
              <h6 class="mb-0 fw-semibold">Temperature</h6>
            </div>
            <div class="chart-body">
              <kendo-chart [style.height.px]="280" *ngIf="metrics.length > 0">
                <kendo-chart-series>
                  <kendo-chart-series-item
                    type="line"
                    [data]="metrics"
                    field="temperature"
                    categoryField="timestamp"
                    [color]="'#F59E0B'"
                    [markers]="{ visible: true, size: 4 }">
                  </kendo-chart-series-item>
                </kendo-chart-series>
                <kendo-chart-category-axis>
                  <kendo-chart-category-axis-item [labels]="{ rotation: -45, font: '11px sans-serif' }">
                  </kendo-chart-category-axis-item>
                </kendo-chart-category-axis>
                <kendo-chart-value-axis>
                  <kendo-chart-value-axis-item [title]="{ text: 'Celsius' }">
                  </kendo-chart-value-axis-item>
                </kendo-chart-value-axis>
                <kendo-chart-tooltip [shared]="true"></kendo-chart-tooltip>
              </kendo-chart>
              <div *ngIf="metrics.length === 0" class="text-center text-muted py-5" data-testid="no-data-message">
                Select equipment to view telemetry data
              </div>
            </div>
          </div>
        </div>

        <!-- GPS Trail -->
        <div class="col-12 col-lg-6">
          <div class="chart-card" data-testid="chart-gps-trail">
            <div class="chart-header">
              <h6 class="mb-0 fw-semibold">GPS Trail</h6>
            </div>
            <div class="chart-body d-flex align-items-center justify-content-center" style="height: 280px;">
              <div class="text-center text-muted">
                <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="#9ca3af" stroke-width="1.5">
                  <path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"/>
                  <circle cx="12" cy="10" r="3"/>
                </svg>
                <div class="mt-2">GPS Map</div>
                <div style="font-size: 12px;">Leaflet integration coming soon</div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .chart-card {
      background: var(--surface-secondary);
      border: 1px solid var(--border-subtle);
      border-radius: var(--radius-lg);
      overflow: hidden;
    }
    .chart-header {
      padding: 16px 20px;
      border-bottom: 1px solid var(--border-subtle);
    }
    .chart-body {
      padding: 16px;
    }
    .btn-group .btn {
      border-radius: var(--radius-sm);
    }
    .btn-group .btn + .btn {
      margin-left: -1px;
    }
  `]
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
