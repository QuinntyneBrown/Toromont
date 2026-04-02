import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-kpi-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './kpi-card.component.html',
  styleUrl: './kpi-card.component.scss'
})
export class KpiCardComponent {
  @Input() label = '';
  @Input() value: string | number = '';
  @Input() trendDirection?: 'up' | 'down' | 'stable';
  @Input() trendValue?: string;
}

