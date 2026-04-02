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
  templateUrl: './ai-insights.component.html',
  styleUrl: './ai-insights.component.scss'
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

