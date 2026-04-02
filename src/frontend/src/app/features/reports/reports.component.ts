import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ChartsModule } from '@progress/kendo-angular-charts';
import { DateInputsModule } from '@progress/kendo-angular-dateinputs';
import { DropDownsModule } from '@progress/kendo-angular-dropdowns';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { ApiService } from '../../core/services/api.service';
import { Equipment } from '../../core/models';
import { HttpResponse } from '@angular/common/http';
import { Subject, takeUntil } from 'rxjs';

interface ReportType {
  key: string;
  label: string;
  icon: string;
  endpoint: string;
}

interface ChartDataItem {
  label: string;
  value: number;
  category?: string;
}

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [CommonModule, ChartsModule, DateInputsModule, DropDownsModule, ButtonsModule],
  template: `
    <div class="container-fluid p-4">
      <h2 class="mb-4 fw-bold">Reports &amp; Analytics</h2>

      <!-- Report Type Cards -->
      <div class="row g-3 mb-4">
        <div *ngFor="let type of reportTypes" class="col-12 col-md-4">
          <div class="report-type-card"
               [class.selected]="selectedReport === type.key"
               (click)="selectReport(type.key)">
            <div class="report-icon">
              <svg *ngIf="type.icon === 'bar'" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <rect x="3" y="12" width="4" height="9"/><rect x="10" y="7" width="4" height="14"/><rect x="17" y="3" width="4" height="18"/>
              </svg>
              <svg *ngIf="type.icon === 'dollar'" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <line x1="12" y1="1" x2="12" y2="23"/><path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"/>
              </svg>
              <svg *ngIf="type.icon === 'repeat'" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <polyline points="17 1 21 5 17 9"/><path d="M3 11V9a4 4 0 0 1 4-4h14"/>
                <polyline points="7 23 3 19 7 15"/><path d="M21 13v2a4 4 0 0 1-4 4H3"/>
              </svg>
            </div>
            <div class="report-label">{{ type.label }}</div>
          </div>
        </div>
      </div>

      <!-- Config Toolbar -->
      <div class="config-toolbar d-flex flex-wrap gap-3 align-items-end mb-4 p-3 bg-white rounded-3 border">
        <div>
          <label class="form-label mb-1" style="font-size: 12px; font-weight: 600;">Date Range</label>
          <kendo-daterange>
            <kendo-dateinput kendoDateRangeStartInput [(value)]="dateStart" placeholder="Start date"></kendo-dateinput>
            <kendo-dateinput kendoDateRangeEndInput [(value)]="dateEnd" placeholder="End date"></kendo-dateinput>
          </kendo-daterange>
        </div>
        <div style="min-width: 220px;">
          <label class="form-label mb-1" style="font-size: 12px; font-weight: 600;">Equipment</label>
          <kendo-dropdownlist
            [data]="equipmentList"
            [textField]="'displayName'"
            [valueField]="'id'"
            [value]="selectedEquipment"
            [valuePrimitive]="false"
            (valueChange)="selectedEquipment = $event"
            [defaultItem]="{ id: '', displayName: 'All Equipment' }">
          </kendo-dropdownlist>
        </div>
        <div>
          <button kendoButton [themeColor]="'primary'" (click)="generateReport()">
            Generate Report
          </button>
        </div>
        <div class="ms-auto d-flex gap-2">
          <button kendoButton [fillMode]="'outline'" (click)="exportReport('pdf')">PDF</button>
          <button kendoButton [fillMode]="'outline'" (click)="exportReport('excel')">Excel</button>
          <button kendoButton [fillMode]="'outline'" (click)="exportReport('csv')">CSV</button>
        </div>
      </div>

      <!-- Charts -->
      <div class="row g-4" *ngIf="barChartData.length > 0 || pieChartData.length > 0">
        <!-- Bar Chart -->
        <div class="col-12 col-lg-7">
          <div class="chart-card">
            <div class="chart-header">
              <h6 class="mb-0 fw-semibold">{{ barChartTitle }}</h6>
            </div>
            <div class="chart-body">
              <kendo-chart [style.height.px]="350">
                <kendo-chart-series>
                  <kendo-chart-series-item
                    type="column"
                    [data]="barChartData"
                    field="value"
                    categoryField="label"
                    [color]="'#FFCD11'">
                  </kendo-chart-series-item>
                </kendo-chart-series>
                <kendo-chart-category-axis>
                  <kendo-chart-category-axis-item [labels]="{ rotation: -45, font: '11px sans-serif' }">
                  </kendo-chart-category-axis-item>
                </kendo-chart-category-axis>
                <kendo-chart-value-axis>
                  <kendo-chart-value-axis-item [title]="{ text: barChartValueLabel }">
                  </kendo-chart-value-axis-item>
                </kendo-chart-value-axis>
                <kendo-chart-tooltip [shared]="true"></kendo-chart-tooltip>
              </kendo-chart>
            </div>
          </div>
        </div>

        <!-- Pie Chart -->
        <div class="col-12 col-lg-5">
          <div class="chart-card">
            <div class="chart-header">
              <h6 class="mb-0 fw-semibold">{{ pieChartTitle }}</h6>
            </div>
            <div class="chart-body">
              <kendo-chart [style.height.px]="350">
                <kendo-chart-series>
                  <kendo-chart-series-item
                    type="pie"
                    [data]="pieChartData"
                    field="value"
                    categoryField="label"
                    [labels]="{ visible: true, content: pieLabel }">
                  </kendo-chart-series-item>
                </kendo-chart-series>
                <kendo-chart-legend [position]="'bottom'"></kendo-chart-legend>
                <kendo-chart-tooltip></kendo-chart-tooltip>
              </kendo-chart>
            </div>
          </div>
        </div>
      </div>

      <div *ngIf="barChartData.length === 0 && pieChartData.length === 0" class="text-center text-muted py-5 bg-white rounded-3 border">
        <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="#9ca3af" stroke-width="1.5">
          <rect x="3" y="12" width="4" height="9"/><rect x="10" y="7" width="4" height="14"/><rect x="17" y="3" width="4" height="18"/>
        </svg>
        <div class="mt-2">Select a report type and click "Generate Report" to view charts</div>
      </div>
    </div>
  `,
  styles: [`
    .report-type-card {
      background: var(--surface-secondary);
      border: 2px solid var(--border-subtle);
      border-radius: var(--radius-lg);
      padding: 20px;
      cursor: pointer;
      display: flex;
      align-items: center;
      gap: 16px;
      transition: all 0.15s;
    }
    .report-type-card:hover {
      border-color: var(--accent-primary);
    }
    .report-type-card.selected {
      border-color: var(--accent-primary);
      background: var(--surface-secondary);
    }
    .report-icon {
      width: 48px;
      height: 48px;
      border-radius: var(--radius-md);
      background: var(--surface-primary);
      display: flex;
      align-items: center;
      justify-content: center;
      color: var(--foreground-secondary);
    }
    .selected .report-icon {
      background: var(--accent-primary);
      color: var(--surface-inverse);
    }
    .report-label {
      font-weight: 600;
      font-size: 15px;
    }
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
  `]
})
export default class ReportsComponent implements OnInit, OnDestroy {
  private api = inject(ApiService);
  private destroy$ = new Subject<void>();

  reportTypes: ReportType[] = [
    { key: 'fleet-utilization', label: 'Fleet Utilization', icon: 'bar', endpoint: '/reports/fleet-utilization' },
    { key: 'maintenance-costs', label: 'Maintenance Costs', icon: 'dollar', endpoint: '/reports/maintenance-costs' },
    { key: 'equipment-lifecycle', label: 'Equipment Lifecycle', icon: 'repeat', endpoint: '/reports/fleet-utilization' }
  ];

  selectedReport = 'fleet-utilization';
  dateStart: Date = new Date(new Date().setMonth(new Date().getMonth() - 3));
  dateEnd: Date = new Date();
  equipmentList: { id: string; displayName: string }[] = [];
  selectedEquipment: { id: string; displayName: string } | null = null;

  barChartData: ChartDataItem[] = [];
  pieChartData: ChartDataItem[] = [];
  barChartTitle = '';
  pieChartTitle = '';
  barChartValueLabel = '';

  pieLabel = (e: { category: string; percentage?: number }) => `${e.category}: ${((e.percentage ?? 0) * 100).toFixed(0)}%`;

  ngOnInit(): void {
    this.loadEquipment();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadEquipment(): void {
    this.api.get<Equipment[]>('/equipment', { take: 999 })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          this.equipmentList = (data || []).map(e => ({
            id: e.id,
            displayName: `${e.make} ${e.model} (${e.serialNumber})`
          }));
        },
        error: (err) => console.error('Failed to load equipment', err)
      });
  }

  selectReport(key: string): void {
    this.selectedReport = key;
  }

  generateReport(): void {
    const reportType = this.reportTypes.find(r => r.key === this.selectedReport);
    if (!reportType) return;

    const body: Record<string, unknown> = {
      startDate: this.dateStart.toISOString(),
      endDate: this.dateEnd.toISOString(),
    };
    if (this.selectedEquipment?.id) {
      body['equipmentId'] = this.selectedEquipment.id;
    }

    this.api.post<{ barChart: ChartDataItem[]; pieChart: ChartDataItem[] }>(reportType.endpoint, body)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          this.barChartData = data.barChart || [];
          this.pieChartData = data.pieChart || [];

          if (this.selectedReport === 'fleet-utilization') {
            this.barChartTitle = 'Utilization by Equipment';
            this.barChartValueLabel = 'Hours';
            this.pieChartTitle = 'Utilization by Type';
          } else if (this.selectedReport === 'maintenance-costs') {
            this.barChartTitle = 'Cost by Equipment';
            this.barChartValueLabel = 'Cost ($)';
            this.pieChartTitle = 'Cost by Category';
          } else {
            this.barChartTitle = 'Equipment Lifecycle';
            this.barChartValueLabel = 'Value';
            this.pieChartTitle = 'Breakdown by Type';
          }
        },
        error: (err) => console.error('Failed to generate report', err)
      });
  }

  exportReport(format: 'pdf' | 'excel' | 'csv'): void {
    const body: Record<string, unknown> = {
      startDate: this.dateStart.toISOString(),
      endDate: this.dateEnd.toISOString(),
    };
    if (this.selectedEquipment?.id) {
      body['equipmentId'] = this.selectedEquipment.id;
    }

    this.api.post<Blob>(`/reports/${this.selectedReport}/export?format=${format}`, body)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (blob) => {
          const url = window.URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          const extensions: Record<string, string> = { pdf: 'pdf', excel: 'xlsx', csv: 'csv' };
          a.download = `${this.selectedReport}-report.${extensions[format]}`;
          a.click();
          window.URL.revokeObjectURL(url);
        },
        error: (err) => console.error('Failed to export report', err)
      });
  }
}
