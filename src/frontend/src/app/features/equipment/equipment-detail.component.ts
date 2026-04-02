import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { IndicatorsModule } from '@progress/kendo-angular-indicators';
import { GridModule } from '@progress/kendo-angular-grid';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import { KpiCardComponent } from '../../shared/components/kpi-card/kpi-card.component';
import { ApiService } from '../../core/services/api.service';
import { ApiResponse, Equipment, WorkOrder, TelemetryEvent } from '../../core/models';

@Component({
  selector: 'app-equipment-detail',
  standalone: true,
  imports: [
    CommonModule, RouterModule,
    ButtonsModule, IndicatorsModule, GridModule,
    BadgeComponent, KpiCardComponent
  ],
  template: `
    <div class="container-fluid py-3" *ngIf="equipment; else loadingTpl">
      <!-- Header -->
      <div class="d-flex justify-content-between align-items-start mb-4 flex-wrap gap-2">
        <div>
          <button class="btn btn-link text-muted ps-0 mb-1" (click)="goBack()">
            &larr; Back to Equipment
          </button>
          <div class="d-flex align-items-center gap-3">
            <h2 class="fw-bold mb-0">{{ equipment.make }} {{ equipment.model }}</h2>
            <app-badge [text]="equipment.status" [variant]="getStatusVariant(equipment.status)"></app-badge>
          </div>
          <p class="text-muted mb-0 mt-1">Serial: {{ equipment.serialNumber }} &bull; {{ equipment.category }}</p>
        </div>
        <div class="d-flex gap-2">
          <button kendoButton themeColor="primary" (click)="scheduleService()">Schedule Service</button>
          <button kendoButton (click)="viewTelemetry()">View Telemetry</button>
        </div>
      </div>

      <!-- KPI Cards -->
      <div class="row g-3 mb-4">
        <div class="col-6 col-lg-3">
          <app-kpi-card label="Current Hours" [value]="(equipment.currentHours | number:'1.0-0') || ''"></app-kpi-card>
        </div>
        <div class="col-6 col-lg-3">
          <app-kpi-card label="Status" [value]="equipment.status"></app-kpi-card>
        </div>
        <div class="col-6 col-lg-3">
          <app-kpi-card label="Year" [value]="equipment.year"></app-kpi-card>
        </div>
        <div class="col-6 col-lg-3">
          <app-kpi-card label="Work Orders" [value]="workOrders.length"></app-kpi-card>
        </div>
      </div>

      <!-- Tabs -->
      <ul class="nav nav-tabs mb-3">
        <li class="nav-item">
          <a class="nav-link" [class.active]="activeTab === 'specs'" (click)="activeTab = 'specs'" role="button">Specifications</a>
        </li>
        <li class="nav-item">
          <a class="nav-link" [class.active]="activeTab === 'service'" (click)="activeTab = 'service'" role="button">Service History</a>
        </li>
        <li class="nav-item">
          <a class="nav-link" [class.active]="activeTab === 'telemetry'" (click)="activeTab = 'telemetry'" role="button">Telemetry Summary</a>
        </li>
      </ul>

      <!-- Specifications Tab -->
      <div *ngIf="activeTab === 'specs'" class="card">
        <div class="card-body">
          <div class="row">
            <div class="col-md-6">
              <table class="table table-borderless mb-0">
                <tbody>
                  <tr>
                    <td class="text-muted fw-semibold" style="width:160px">Make</td>
                    <td>{{ equipment.make }}</td>
                  </tr>
                  <tr>
                    <td class="text-muted fw-semibold">Model</td>
                    <td>{{ equipment.model }}</td>
                  </tr>
                  <tr>
                    <td class="text-muted fw-semibold">Year</td>
                    <td>{{ equipment.year }}</td>
                  </tr>
                  <tr>
                    <td class="text-muted fw-semibold">Serial Number</td>
                    <td>{{ equipment.serialNumber }}</td>
                  </tr>
                  <tr>
                    <td class="text-muted fw-semibold">Category</td>
                    <td>{{ equipment.category }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
            <div class="col-md-6">
              <table class="table table-borderless mb-0">
                <tbody>
                  <tr>
                    <td class="text-muted fw-semibold" style="width:160px">Current Hours</td>
                    <td>{{ equipment.currentHours | number:'1.0-0' }}</td>
                  </tr>
                  <tr>
                    <td class="text-muted fw-semibold">Status</td>
                    <td>
                      <app-badge [text]="equipment.status" [variant]="getStatusVariant(equipment.status)"></app-badge>
                    </td>
                  </tr>
                  <tr>
                    <td class="text-muted fw-semibold">GPS Latitude</td>
                    <td>{{ equipment.locationLat ?? 'N/A' }}</td>
                  </tr>
                  <tr>
                    <td class="text-muted fw-semibold">GPS Longitude</td>
                    <td>{{ equipment.locationLng ?? 'N/A' }}</td>
                  </tr>
                  <tr>
                    <td class="text-muted fw-semibold">Tenant ID</td>
                    <td>{{ equipment.tenantId }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          <!-- Mini Map -->
          <div *ngIf="equipment.locationLat && equipment.locationLng" class="mt-3">
            <h6 class="fw-semibold">Location</h6>
            <div class="border rounded" style="height:250px;background:#e5e7eb;display:flex;align-items:center;justify-content:center;">
              <div class="text-center text-muted">
                <div style="font-size:2rem">&#128205;</div>
                <div>{{ equipment.locationLat | number:'1.4-4' }}, {{ equipment.locationLng | number:'1.4-4' }}</div>
                <a [href]="'https://maps.google.com/?q=' + equipment.locationLat + ',' + equipment.locationLng"
                   target="_blank" class="btn btn-sm btn-outline-secondary mt-2">Open in Google Maps</a>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Service History Tab -->
      <div *ngIf="activeTab === 'service'">
        <div *ngIf="workOrders.length === 0" class="card">
          <div class="card-body text-center text-muted py-5">No service history found for this equipment.</div>
        </div>
        <kendo-grid *ngIf="workOrders.length > 0" [data]="workOrders" [pageable]="false" [style.width]="'100%'">
          <kendo-grid-column field="id" title="WO #" [width]="100">
            <ng-template kendoGridCellTemplate let-dataItem>
              {{ dataItem.id | slice:0:8 }}
            </ng-template>
          </kendo-grid-column>
          <kendo-grid-column field="title" title="Title" [width]="200"></kendo-grid-column>
          <kendo-grid-column field="priority" title="Priority" [width]="100">
            <ng-template kendoGridCellTemplate let-dataItem>
              <app-badge [text]="dataItem.priority" [variant]="getPriorityVariant(dataItem.priority)"></app-badge>
            </ng-template>
          </kendo-grid-column>
          <kendo-grid-column field="status" title="Status" [width]="120">
            <ng-template kendoGridCellTemplate let-dataItem>
              <app-badge [text]="dataItem.status" [variant]="getWOStatusVariant(dataItem.status)"></app-badge>
            </ng-template>
          </kendo-grid-column>
          <kendo-grid-column field="scheduledDate" title="Scheduled" [width]="130">
            <ng-template kendoGridCellTemplate let-dataItem>
              {{ dataItem.scheduledDate ? (dataItem.scheduledDate | date:'mediumDate') : 'Unscheduled' }}
            </ng-template>
          </kendo-grid-column>
          <kendo-grid-column field="createdAt" title="Created" [width]="130">
            <ng-template kendoGridCellTemplate let-dataItem>
              {{ dataItem.createdAt | date:'mediumDate' }}
            </ng-template>
          </kendo-grid-column>
        </kendo-grid>
      </div>

      <!-- Telemetry Tab -->
      <div *ngIf="activeTab === 'telemetry'">
        <div *ngIf="telemetryEvents.length === 0" class="card">
          <div class="card-body text-center text-muted py-5">No telemetry data available.</div>
        </div>
        <div *ngIf="telemetryEvents.length > 0" class="row g-3">
          <div *ngFor="let event of telemetryEvents" class="col-sm-6 col-lg-4">
            <div class="card">
              <div class="card-body">
                <div class="text-muted small text-uppercase fw-semibold">{{ event.eventType }}</div>
                <div class="fs-3 fw-bold mt-1">{{ event.value | number:'1.1-2' }} <span class="fs-6 text-muted">{{ event.unit }}</span></div>
                <div class="text-muted small mt-2">{{ event.timestamp | date:'medium' }}</div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <ng-template #loadingTpl>
      <div class="d-flex justify-content-center align-items-center py-5">
        <kendo-loader type="infinite-spinner" size="large"></kendo-loader>
      </div>
    </ng-template>
  `,
  styles: [`
    :host { display: block; }
    .nav-link { cursor: pointer; }
    .nav-link.active { font-weight: 600; }
  `]
})
export default class EquipmentDetailComponent implements OnInit {
  equipment: Equipment | null = null;
  workOrders: WorkOrder[] = [];
  telemetryEvents: TelemetryEvent[] = [];
  activeTab: 'specs' | 'service' | 'telemetry' = 'specs';
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
    this.api.get<ApiResponse<Equipment>>(`/equipment/${this.equipmentId}`).subscribe({
      next: (res) => { this.equipment = res.data; },
      error: (err) => { console.error('Failed to load equipment', err); }
    });
  }

  loadWorkOrders(): void {
    this.api.get<ApiResponse<WorkOrder[]>>('/work-orders', { skip: 0, take: 50, equipmentId: this.equipmentId }).subscribe({
      next: (res) => { this.workOrders = res.data || []; },
      error: () => { this.workOrders = []; }
    });
  }

  loadTelemetry(): void {
    this.api.get<ApiResponse<TelemetryEvent[]>>(`/equipment/${this.equipmentId}/telemetry`, { skip: 0, take: 20 }).subscribe({
      next: (res) => { this.telemetryEvents = res.data || []; },
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
      case 'Active': return 'success';
      case 'InService': return 'warning';
      case 'Down': return 'error';
      case 'Retired': return 'info';
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
