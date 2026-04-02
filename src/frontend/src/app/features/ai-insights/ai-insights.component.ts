import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { GridModule } from '@progress/kendo-angular-grid';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { KpiCardComponent } from '../../shared/components/kpi-card/kpi-card.component';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import { ApiService } from '../../core/services/api.service';
import { AIPrediction, AnomalyDetection } from '../../core/models';
import { Subject, takeUntil } from 'rxjs';

interface DashboardStats {
  totalPredictions: number;
  highPriority: number;
  activeAnomalies: number;
  estimatedCostSavings: number;
}

interface PredictionRow extends AIPrediction {
  equipmentName?: string;
  component?: string;
  timeframe?: string;
  priority?: string;
}

@Component({
  selector: 'app-ai-insights',
  standalone: true,
  imports: [CommonModule, GridModule, ButtonsModule, KpiCardComponent, BadgeComponent],
  template: `
    <div class="container-fluid p-4">
      <h2 class="mb-4 fw-bold">AI Insights</h2>

      <!-- KPI Cards -->
      <div class="row g-3 mb-4">
        <div class="col-12 col-sm-6 col-lg-3">
          <app-kpi-card label="Total Predictions" [value]="stats.totalPredictions"></app-kpi-card>
        </div>
        <div class="col-12 col-sm-6 col-lg-3">
          <div class="kpi-highlight kpi-red">
            <app-kpi-card label="High Priority" [value]="stats.highPriority"></app-kpi-card>
          </div>
        </div>
        <div class="col-12 col-sm-6 col-lg-3">
          <div class="kpi-highlight kpi-warning">
            <app-kpi-card label="Active Anomalies" [value]="stats.activeAnomalies"></app-kpi-card>
          </div>
        </div>
        <div class="col-12 col-sm-6 col-lg-3">
          <div class="kpi-highlight kpi-green">
            <app-kpi-card label="Est. Cost Savings" [value]="'$' + formatNumber(stats.estimatedCostSavings)"></app-kpi-card>
          </div>
        </div>
      </div>

      <!-- Two Column Section -->
      <div class="row g-4">
        <!-- Predictions Grid -->
        <div class="col-12 col-xl">
          <div class="card">
            <div class="card-header bg-white">
              <h5 class="mb-0 fw-semibold">Predictive Maintenance</h5>
            </div>
            <div class="card-body p-0">
              <kendo-grid [data]="predictions" [pageable]="true" [pageSize]="10" [sortable]="true" [style.font-size.px]="13">
                <kendo-grid-column field="equipmentName" title="Equipment" [width]="160">
                  <ng-template kendoGridCellTemplate let-dataItem>
                    {{ dataItem.equipmentName || 'Equipment #' + dataItem.equipmentId }}
                  </ng-template>
                </kendo-grid-column>
                <kendo-grid-column field="component" title="Component" [width]="140">
                  <ng-template kendoGridCellTemplate let-dataItem>
                    {{ dataItem.predictionType }}
                  </ng-template>
                </kendo-grid-column>
                <kendo-grid-column field="confidence" title="Confidence" [width]="160">
                  <ng-template kendoGridCellTemplate let-dataItem>
                    <div class="d-flex align-items-center gap-2">
                      <div class="progress flex-grow-1" style="height: 6px;">
                        <div class="progress-bar"
                             [style.width.%]="dataItem.confidence"
                             [ngClass]="{
                               'bg-danger': dataItem.confidence >= 80,
                               'bg-warning': dataItem.confidence >= 50 && dataItem.confidence < 80,
                               'bg-secondary': dataItem.confidence < 50
                             }">
                        </div>
                      </div>
                      <span style="min-width: 38px; font-size: 12px;">{{ dataItem.confidence }}%</span>
                    </div>
                  </ng-template>
                </kendo-grid-column>
                <kendo-grid-column field="recommendedAction" title="Recommended Action" [width]="200">
                  <ng-template kendoGridCellTemplate let-dataItem>
                    {{ dataItem.recommendedAction || 'N/A' }}
                  </ng-template>
                </kendo-grid-column>
                <kendo-grid-column field="predictedDate" title="Timeframe" [width]="120">
                  <ng-template kendoGridCellTemplate let-dataItem>
                    {{ dataItem.predictedDate ? (dataItem.predictedDate | date:'mediumDate') : 'TBD' }}
                  </ng-template>
                </kendo-grid-column>
                <kendo-grid-column title="Priority" [width]="120">
                  <ng-template kendoGridCellTemplate let-dataItem>
                    <app-badge
                      [text]="getPriorityLabel(dataItem.confidence)"
                      [variant]="getPriorityVariant(dataItem.confidence)">
                    </app-badge>
                  </ng-template>
                </kendo-grid-column>
                <kendo-grid-column title="Actions" [width]="100">
                  <ng-template kendoGridCellTemplate let-dataItem>
                    <button class="btn btn-sm btn-outline-secondary" (click)="dismissPrediction(dataItem)">
                      Dismiss
                    </button>
                  </ng-template>
                </kendo-grid-column>
              </kendo-grid>
            </div>
          </div>
        </div>

        <!-- Anomaly Alerts Panel -->
        <div class="col-12 col-xl-auto" style="width: 380px;">
          <div class="card h-100">
            <div class="card-header bg-white">
              <h5 class="mb-0 fw-semibold">Anomaly Alerts</h5>
            </div>
            <div class="card-body p-0" style="max-height: 600px; overflow-y: auto;">
              <div *ngIf="anomalies.length === 0" class="text-center text-muted py-4">
                No anomalies detected
              </div>
              <div *ngFor="let anomaly of anomalies" class="anomaly-item px-3 py-3 border-bottom">
                <div class="d-flex align-items-start gap-3">
                  <div class="anomaly-icon" [ngClass]="'icon-' + anomaly.severity.toLowerCase()">
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                      <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/>
                      <line x1="12" y1="9" x2="12" y2="13"/>
                      <line x1="12" y1="17" x2="12.01" y2="17"/>
                    </svg>
                  </div>
                  <div class="flex-grow-1">
                    <div class="fw-semibold" style="font-size: 13px;">{{ anomaly.parameterName }}</div>
                    <div class="text-muted" style="font-size: 12px;">
                      Expected: {{ anomaly.expectedValue | number:'1.1-1' }} |
                      Actual: {{ anomaly.actualValue | number:'1.1-1' }}
                    </div>
                    <div class="d-flex justify-content-between mt-1">
                      <span class="deviation" [ngClass]="'text-' + (anomaly.deviationPercentage > 20 ? 'danger' : 'warning')"
                            style="font-size: 12px; font-weight: 600;">
                        {{ anomaly.deviationPercentage > 0 ? '+' : '' }}{{ anomaly.deviationPercentage | number:'1.1-1' }}% deviation
                      </span>
                      <span class="text-muted" style="font-size: 11px;">{{ getTimeAgo(anomaly.detectedAt) }}</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .card {
      border: 1px solid var(--border-subtle);
      border-radius: var(--radius-lg);
      overflow: hidden;
    }
    .card-header {
      padding: 16px 20px;
      border-bottom: 1px solid var(--border-subtle);
    }
    .kpi-highlight {
      border-radius: var(--radius-lg);
    }
    .kpi-red :host ::ng-deep .kpi-value,
    .kpi-red .kpi-value { color: var(--status-error); }
    .kpi-warning :host ::ng-deep .kpi-value,
    .kpi-warning .kpi-value { color: var(--status-warning); }
    .kpi-green :host ::ng-deep .kpi-value,
    .kpi-green .kpi-value { color: var(--status-success); }

    .anomaly-icon {
      width: 36px;
      height: 36px;
      border-radius: var(--radius-md);
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
    }
    .icon-critical { background: rgba(239,68,68,0.1); color: var(--status-error); }
    .icon-high { background: rgba(249,115,22,0.1); color: var(--status-warning); }
    .icon-medium { background: rgba(234,179,8,0.1); color: var(--status-warning); }
    .icon-low { background: rgba(107,114,128,0.1); color: var(--foreground-secondary); }

    .anomaly-item:last-child {
      border-bottom: none !important;
    }
    .anomaly-item:hover {
      background: var(--surface-primary);
    }

    .progress {
      border-radius: var(--radius-sm);
      background: var(--border-subtle);
    }
  `]
})
export default class AiInsightsComponent implements OnInit, OnDestroy {
  private api = inject(ApiService);
  private destroy$ = new Subject<void>();

  stats: DashboardStats = {
    totalPredictions: 0,
    highPriority: 0,
    activeAnomalies: 0,
    estimatedCostSavings: 0
  };

  predictions: PredictionRow[] = [];
  anomalies: AnomalyDetection[] = [];

  ngOnInit(): void {
    this.loadData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadData(): void {
    this.api.get<DashboardStats>('/ai/dashboard-stats')
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => this.stats = data,
        error: (err) => console.error('Failed to load AI stats', err)
      });

    this.api.get<PredictionRow[]>('/ai/predictions')
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => this.predictions = data || [],
        error: (err) => console.error('Failed to load predictions', err)
      });

    this.api.get<AnomalyDetection[]>('/ai/anomalies')
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => this.anomalies = data || [],
        error: (err) => console.error('Failed to load anomalies', err)
      });
  }

  dismissPrediction(prediction: PredictionRow): void {
    this.api.put(`/ai/predictions/${prediction.id}/dismiss`, {})
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.predictions = this.predictions.filter(p => p.id !== prediction.id);
          if (this.stats.totalPredictions > 0) this.stats.totalPredictions--;
          if (prediction.confidence >= 80 && this.stats.highPriority > 0) this.stats.highPriority--;
        },
        error: (err) => console.error('Failed to dismiss prediction', err)
      });
  }

  getPriorityLabel(confidence: number): string {
    if (confidence >= 80) return 'High Priority';
    if (confidence >= 50) return 'Medium';
    return 'Low Confidence';
  }

  getPriorityVariant(confidence: number): 'error' | 'warning' | 'info' {
    if (confidence >= 80) return 'error';
    if (confidence >= 50) return 'warning';
    return 'info';
  }

  formatNumber(value: number): string {
    if (value >= 1000000) return (value / 1000000).toFixed(1) + 'M';
    if (value >= 1000) return (value / 1000).toFixed(1) + 'K';
    return value.toFixed(0);
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
