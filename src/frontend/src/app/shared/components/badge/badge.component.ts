import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-badge',
  standalone: true,
  imports: [CommonModule],
  template: `<span class="status-badge" [ngClass]="'badge-' + variant">{{ text }}</span>`,
  styles: [`
    .status-badge {
      display: inline-flex;
      align-items: center;
      padding: 2px 10px;
      border-radius: 12px;
      font-size: 12px;
      font-weight: 600;
      line-height: 20px;
    }
    .badge-success {
      background-color: rgba(16, 185, 129, 0.1);
      color: var(--status-success);
    }
    .badge-warning {
      background-color: rgba(245, 158, 11, 0.1);
      color: var(--status-warning);
    }
    .badge-error {
      background-color: rgba(239, 68, 68, 0.1);
      color: var(--status-error);
    }
    .badge-info {
      background-color: rgba(59, 130, 246, 0.1);
      color: var(--status-info);
    }
  `]
})
export class BadgeComponent {
  @Input() text = '';
  @Input() variant: 'success' | 'warning' | 'error' | 'info' = 'info';
}
