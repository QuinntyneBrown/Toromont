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
  templateUrl: './reports.component.html',
  styleUrl: './reports.component.scss'
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
    this.api.get<any>('/equipment', { take: 999 })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.equipmentList = (res.items || []).map((e: any) => ({
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

    this.api.post<any>(reportType.endpoint, body)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          const dataPoints: ChartDataItem[] = data.dataPoints || [];
          this.barChartData = dataPoints;
          // Group dataPoints by category for pie chart
          const categoryMap = new Map<string, number>();
          for (const dp of dataPoints) {
            const cat = dp.category || 'Other';
            categoryMap.set(cat, (categoryMap.get(cat) || 0) + dp.value);
          }
          this.pieChartData = Array.from(categoryMap.entries()).map(([label, value]) => ({ label, value }));

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

