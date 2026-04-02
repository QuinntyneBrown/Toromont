import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { KpiCardComponent } from '../../shared/components/kpi-card/kpi-card.component';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import { ApiService } from '../../core/services/api.service';
import { AIPrediction, AnomalyDetection } from '../../core/models';
import { Subject, takeUntil } from 'rxjs';

interface DashboardStats {
  totalPredictions: number;
  highPriority: number;
  anomalyCount: number;
  estimatedSavings: number;
}

interface PredictionRow extends AIPrediction {
  equipmentName?: string;
}

@Component({
  selector: 'app-ai-insights',
  standalone: true,
  imports: [CommonModule, ButtonsModule, KpiCardComponent, BadgeComponent],
  template: `
    <div class="container-fluid p-4">
      <h2 class="mb-4 fw-bold">AI Insights</h2>

      <!-- KPI Cards -->
      <div class="row g-3 mb-4">
        <div class="col-12 col-sm-6 col-lg-3" data-testid="kpi-total-predictions">
          <app-kpi-card label="Total Predictions" [value]="stats.totalPredictions"></app-kpi-card>
        </div>
        <div class="col-12 col-sm-6 col-lg-3" data-testid="kpi-high-priority">
          <div class="kpi-highlight kpi-red">
            <app-kpi-card label="High Priority" [value]="stats.highPriority"></app-kpi-card>
          </div>
        </div>
        <div class="col-12 col-sm-6 col-lg-3" data-testid="kpi-anomalies">
          <div class="kpi-highlight kpi-warning">
            <app-kpi-card label="Active Anomalies" [value]="stats.anomalyCount"></app-kpi-card>
          </div>
        </div>
        <div class="col-12 col-sm-6 col-lg-3" data-testid="kpi-cost-savings">
          <div class="kpi-highlight kpi-green">
            <app-kpi-card label="Est. Cost Savings" [value]="'$' + formatNumber(stats.estimatedSavings)"></app-kpi-card>
          </div>
        </div>
      </div>

      <!-- Two Column Section -->
      <div class="row g-4">
        <!-- Predictions Grid -->
        <div class="col-12 col-xl">
          <div class="card" data-testid="predictions-grid">
            <div class="card-header bg-white">
              <h5 class="mb-0 fw-semibold">Predictive Maintenance</h5>
            </div>
            <div class="card-body p-0">
              <div class="table-responsive">
                <table class="table table-hover mb-0" style="font-size: 13px;">
                  <thead>
                    <tr>
                      <th class="sortable-header" style="width: 160px;" (click)="sortBy('equipmentId')">
                        Equipment <span *ngIf="sortField === 'equipmentId'">{{ sortDir === 'asc' ? '▲' : '▼' }}</span>
                      </th>
                      <th class="sortable-header" style="width: 140px;" (click)="sortBy('component')">
                        Component <span *ngIf="sortField === 'component'">{{ sortDir === 'asc' ? '▲' : '▼' }}</span>
                      </th>
                      <th class="sortable-header" style="width: 160px;" (click)="sortBy('confidenceScore')">
                        Confidence <span *ngIf="sortField === 'confidenceScore'">{{ sortDir === 'asc' ? '▲' : '▼' }}</span>
                      </th>
                      <th style="width: 200px;">Recommended Action</th>
                      <th class="sortable-header" style="width: 120px;" (click)="sortBy('timeframe')">
                        Timeframe <span *ngIf="sortField === 'timeframe'">{{ sortDir === 'asc' ? '▲' : '▼' }}</span>
                      </th>
                      <th class="sortable-header" style="width: 120px;" (click)="sortBy('priority')">
                        Priority <span *ngIf="sortField === 'priority'">{{ sortDir === 'asc' ? '▲' : '▼' }}</span>
                      </th>
                      <th style="width: 100px;">Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr *ngFor="let dataItem of pagedPredictions" data-testid="prediction-row">
                      <td data-testid="pred-equipment">{{ dataItem.equipment?.name || 'Equipment #' + dataItem.equipmentId }}</td>
                      <td data-testid="pred-component">{{ dataItem.component }}</td>
                      <td data-testid="pred-confidence">
                        <div class="d-flex align-items-center gap-2">
                          <div class="progress flex-grow-1" style="height: 6px;">
                            <div class="progress-bar"
                                 [style.width.%]="dataItem.confidenceScore * 100"
                                 [ngClass]="{
                                   'bg-danger': dataItem.confidenceScore >= 0.8,
                                   'bg-warning': dataItem.confidenceScore >= 0.5 && dataItem.confidenceScore < 0.8,
                                   'bg-secondary': dataItem.confidenceScore < 0.5
                                 }">
                            </div>
                          </div>
                          <span style="min-width: 38px; font-size: 12px;">{{ (dataItem.confidenceScore * 100 | number:'1.0-0') }}%</span>
                        </div>
                      </td>
                      <td>{{ dataItem.recommendedAction || 'N/A' }}</td>
                      <td data-testid="pred-timeframe">{{ dataItem.timeframe || 'TBD' }}</td>
                      <td>
                        <span data-testid="priority-badge">
                          <app-badge
                            [text]="dataItem.priority || 'Low'"
                            [variant]="getPriorityVariant(dataItem.confidenceScore * 100)">
                          </app-badge>
                        </span>
                      </td>
                      <td>
                        <button class="btn btn-sm btn-outline-secondary" (click)="dismissPrediction(dataItem)">
                          Dismiss
                        </button>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
              <!-- Pagination -->
              <div *ngIf="predictions.length > pageSize" class="d-flex justify-content-between align-items-center px-3 py-2 border-top">
                <span style="font-size: 12px;" class="text-muted">{{ (currentPage - 1) * pageSize + 1 }}–{{ currentPage * pageSize < predictions.length ? currentPage * pageSize : predictions.length }} of {{ predictions.length }}</span>
                <div class="d-flex gap-1">
                  <button class="btn btn-sm btn-outline-secondary" [disabled]="currentPage === 1" (click)="currentPage = currentPage - 1">Prev</button>
                  <button class="btn btn-sm btn-outline-secondary" [disabled]="currentPage >= totalPages" (click)="currentPage = currentPage + 1">Next</button>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Anomaly Alerts Panel -->
        <div class="col-12 col-xl-auto" style="width: 380px;" data-testid="anomaly-alerts">
          <div class="card h-100">
            <div class="card-header bg-white">
              <h5 class="mb-0 fw-semibold">Anomaly Alerts</h5>
            </div>
            <div class="card-body p-0" style="max-height: 600px; overflow-y: auto;">
              <div *ngIf="anomalies.length === 0" class="text-center text-muted py-4">
                No anomalies detected
              </div>
              <div *ngFor="let anomaly of anomalies" class="anomaly-item px-3 py-3 border-bottom" data-testid="anomaly-alert">
                <div class="d-flex align-items-start gap-3">
                  <div class="anomaly-icon" [ngClass]="'icon-' + anomaly.severity.toLowerCase()" data-testid="anomaly-severity">
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                      <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/>
                      <line x1="12" y1="9" x2="12" y2="13"/>
                      <line x1="12" y1="17" x2="12.01" y2="17"/>
                    </svg>
                  </div>
                  <div class="flex-grow-1">
                    <div class="fw-semibold" style="font-size: 13px;"><span data-testid="anomaly-type">{{ anomaly.metricName }}</span> — <span data-testid="anomaly-equipment">{{ anomaly.equipment?.name || 'Equipment' }}</span></div>
                    <div class="text-muted" style="font-size: 12px;">
                      Expected: {{ anomaly.expectedValue | number:'1.1-1' }} |
                      Actual: {{ anomaly.actualValue | number:'1.1-1' }}
                    </div>
                    <div class="d-flex justify-content-between mt-1">
                      <span class="deviation" [ngClass]="'text-' + (anomaly.deviationSigma > 2 ? 'danger' : 'warning')"
                            style="font-size: 12px; font-weight: 600;">
                        {{ anomaly.deviationSigma > 0 ? '+' : '' }}{{ anomaly.deviationSigma | number:'1.1-1' }}σ deviation
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

    .table th {
      background: var(--surface-primary, #f8f9fa);
      border-bottom: 2px solid var(--border-subtle, #dee2e6);
      font-weight: 600;
      font-size: 12px;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      color: var(--foreground-secondary, #6c757d);
      padding: 10px 12px;
      white-space: nowrap;
    }
    .table td {
      padding: 10px 12px;
      vertical-align: middle;
      border-bottom: 1px solid var(--border-subtle, #dee2e6);
    }
    .sortable-header {
      cursor: pointer;
      user-select: none;
    }
    .sortable-header:hover {
      background: var(--border-subtle, #e9ecef);
    }
  `]
})
export default class AiInsightsComponent implements OnInit, OnDestroy {
  private api = inject(ApiService);
  private destroy$ = new Subject<void>();

  stats: DashboardStats = {
    totalPredictions: 0,
    highPriority: 0,
    anomalyCount: 0,
    estimatedSavings: 0
  };

  predictions: PredictionRow[] = [];
  anomalies: AnomalyDetection[] = [];

  // Sorting & pagination state
  sortField = '';
  sortDir: 'asc' | 'desc' = 'asc';
  pageSize = 10;
  currentPage = 1;

  get totalPages(): number {
    return Math.ceil(this.predictions.length / this.pageSize);
  }

  get pagedPredictions(): PredictionRow[] {
    let data = [...this.predictions];
    if (this.sortField) {
      data.sort((a: any, b: any) => {
        const valA = a[this.sortField] ?? '';
        const valB = b[this.sortField] ?? '';
        if (valA < valB) return this.sortDir === 'asc' ? -1 : 1;
        if (valA > valB) return this.sortDir === 'asc' ? 1 : -1;
        return 0;
      });
    }
    const start = (this.currentPage - 1) * this.pageSize;
    return data.slice(start, start + this.pageSize);
  }

  sortBy(field: string): void {
    if (this.sortField === field) {
      this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortField = field;
      this.sortDir = 'asc';
    }
    this.currentPage = 1;
  }

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
          if ((prediction as any).confidenceScore >= 0.8 && this.stats.highPriority > 0) this.stats.highPriority--;
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
    if (!value && value !== 0) return '0';
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
