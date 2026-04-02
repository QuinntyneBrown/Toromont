import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { IndicatorsModule } from '@progress/kendo-angular-indicators';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import { KpiCardComponent } from '../../shared/components/kpi-card/kpi-card.component';
import { ApiService } from '../../core/services/api.service';
import { ApiResponse, Equipment, WorkOrder, TelemetryEvent } from '../../core/models';

@Component({
  selector: 'app-equipment-detail',
  standalone: true,
  imports: [
    CommonModule, RouterModule,
    ButtonsModule, IndicatorsModule,
    BadgeComponent, KpiCardComponent
  ],
  templateUrl: './equipment-detail.component.html',
  styleUrl: './equipment-detail.component.scss'
})
export default class EquipmentDetailComponent implements OnInit {
  equipment: Equipment | null = null;
  workOrders: WorkOrder[] = [];
  telemetryEvents: TelemetryEvent[] = [];
  latestTelemetry: any = null;
  private equipmentId = '';

  constructor(
    private api: ApiService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.equipmentId = this.route.snapshot.paramMap.get('id') || '';
    if (this.equipmentId) {
      this.loadEquipment();
      this.loadWorkOrders();
      this.loadTelemetry();
    }
  }

  loadEquipment(): void {
    this.api.get<Equipment>(`/equipment/${this.equipmentId}`).subscribe({
      next: (res) => { this.equipment = res; },
      error: (err) => { console.error('Failed to load equipment', err); }
    });
  }

  loadWorkOrders(): void {
    this.api.get<any>('/work-orders', { skip: 0, take: 50, equipmentId: this.equipmentId }).subscribe({
      next: (res) => { this.workOrders = res.items || []; },
      error: () => { this.workOrders = []; }
    });
  }

  loadTelemetry(): void {
    this.api.get<any>(`/telemetry/${this.equipmentId}/latest`).subscribe({
      next: (res) => { this.latestTelemetry = res; },
      error: () => { this.telemetryEvents = []; }
    });
  }

  goBack(): void {
    this.router.navigate(['/equipment']);
  }

  scheduleService(): void {
    this.router.navigate(['/service'], { queryParams: { equipmentId: this.equipmentId } });
  }

  viewTelemetry(): void {
    this.router.navigate(['/telemetry', this.equipmentId]);
  }

  getStatusVariant(status: string): 'success' | 'warning' | 'error' | 'info' {
    switch (status) {
      case 'Operational': return 'success';
      case 'NeedsService': return 'warning';
      case 'OutOfService': return 'error';
      case 'Idle': return 'info';
      default: return 'info';
    }
  }

  getPriorityVariant(priority: string): 'success' | 'warning' | 'error' | 'info' {
    switch (priority) {
      case 'Critical': return 'error';
      case 'High': return 'warning';
      case 'Medium': return 'info';
      case 'Low': return 'info';
      default: return 'info';
    }
  }

  getWOStatusVariant(status: string): 'success' | 'warning' | 'error' | 'info' {
    switch (status) {
      case 'Completed': return 'success';
      case 'InProgress': return 'warning';
      case 'Open': return 'info';
      case 'Cancelled': return 'error';
      default: return 'info';
    }
  }
}

