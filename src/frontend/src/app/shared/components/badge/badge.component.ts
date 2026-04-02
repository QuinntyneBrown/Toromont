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
      justify-content: center;
      padding: 4px 10px;
      border-radius: 12px;
      font-size: 12px;
      font-weight: 500;
      color: var(--foreground-inverse);
    }
    .badge-success {
      background-color: var(--status-success);
    }
    .badge-warning {
      background-color: var(--status-warning);
    }
    .badge-error {
      background-color: var(--status-error);
    }
    .badge-info {
      background-color: var(--status-info);
    }
  `]
})
export class BadgeComponent {
  @Input() text = '';
  @Input() variant: 'success' | 'warning' | 'error' | 'info' = 'info';
}
