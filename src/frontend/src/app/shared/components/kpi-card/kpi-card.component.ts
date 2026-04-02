import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-kpi-card',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="kpi-card">
      <div class="kpi-label">{{ label }}</div>
      <div class="kpi-value" data-testid="kpi-value">{{ value }}</div>
      <div class="kpi-trend" data-testid="kpi-trend" *ngIf="trendDirection" [ngClass]="'trend-' + trendDirection">
        <svg *ngIf="trendDirection === 'up'" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <polyline points="23 6 13.5 15.5 8.5 10.5 1 18"/>
          <polyline points="17 6 23 6 23 12"/>
        </svg>
        <svg *ngIf="trendDirection === 'down'" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <polyline points="23 18 13.5 8.5 8.5 13.5 1 6"/>
          <polyline points="17 18 23 18 23 12"/>
        </svg>
        <span *ngIf="trendDirection === 'stable'">--</span>
        <span class="trend-value">{{ trendValue }}</span>
      </div>
    </div>
  `,
  styles: [`
    .kpi-card {
      background-color: var(--surface-secondary);
      border: 1px solid var(--border-subtle);
      border-radius: var(--radius-md);
      padding: 20px;
      display: flex;
      flex-direction: column;
      gap: 8px;
    }
    .kpi-label {
      font-size: 13px;
      color: var(--foreground-secondary);
      font-weight: 500;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }
    .kpi-value {
      font-size: 32px;
      font-weight: 700;
      color: var(--foreground-primary);
      font-family: var(--font-data);
    }
    .kpi-trend {
      display: flex;
      align-items: center;
      gap: 4px;
      font-size: 13px;
      font-weight: 500;
    }
    .trend-up { color: var(--status-success); }
    .trend-down { color: var(--status-error); }
    .trend-stable { color: var(--foreground-secondary); }
    .trend-value { font-family: var(--font-data); }
  `]
})
export class KpiCardComponent {
  @Input() label = '';
  @Input() value: string | number = '';
  @Input() trendDirection?: 'up' | 'down' | 'stable';
  @Input() trendValue?: string;
}
